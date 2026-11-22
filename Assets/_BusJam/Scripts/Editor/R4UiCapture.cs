#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using BusJam.Core;
using BusJam.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>R4 review: Play mode Game View captures of every UI screen (menu, popups, results) at fixed sizes.</summary>
public static class R4UiCapture
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private struct Shot { public int W, H, Top, Bottom; public string Name, Screen; public float Wait; }
    private static Shot[] shots;
    private static string folder;
    private static EditorWindow view;
    private static object group;
    private static int originalIndex, customIndex = -1, index, phase;
    private static double readyAt;

    /// <summary>spec: "name,w,h,safeTop,safeBottom,screen,waitSeconds;..." screen = start|settings|reset|nolives|ready|hud|pause|confirm|fail|complete|collect|retry|none</summary>
    public static string Start(string outFolder, string spec)
    {
        if (!EditorApplication.isPlaying) return "Play mode required.";
        Cleanup();
        folder = outFolder; Directory.CreateDirectory(folder);
        string[] parts = spec.Split(';');
        shots = new Shot[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            string[] p = parts[i].Split(',');
            shots[i] = new Shot { Name = p[0], W = int.Parse(p[1]), H = int.Parse(p[2]), Top = int.Parse(p[3]), Bottom = int.Parse(p[4]),
                Screen = p[5], Wait = float.Parse(p[6], CultureInfo.InvariantCulture) };
        }
        var gvType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        view = EditorWindow.GetWindow(gvType);
        originalIndex = (int)gvType.GetProperty("selectedSizeIndex", Flags).GetValue(view);
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(Editor).Assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
        group = sizesType.GetProperty("currentGroup", Flags).GetValue(singleton.GetProperty("instance", Flags).GetValue(null));
        index = phase = 0;
        File.Delete(folder + "/done.txt");
        EditorApplication.update += Step;
        return "Queued " + shots.Length;
    }

    private static void SetSize(int w, int h)
    {
        if (customIndex >= 0) group.GetType().GetMethod("RemoveCustomSize", Flags).Invoke(group, new object[] { customIndex });
        customIndex = (int)group.GetType().GetMethod("GetTotalCount", Flags).Invoke(group, null);
        var sizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        var enumType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        group.GetType().GetMethod("AddCustomSize", Flags).Invoke(group, new[] { Activator.CreateInstance(sizeType, Flags, null, new object[] { Enum.Parse(enumType, "FixedResolution"), w, h, "R4" }, null) });
        view.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(view, customIndex);
    }

    private static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Flags).Invoke(target, args);

    private static void State(GameManager gm, GameState state) => Call(gm, "SetState", state);

    private static void Show(Shot s)
    {
        var gm = Object.FindObjectOfType<GameManager>();
        var ui = Object.FindObjectOfType<ToolkitGameUI>();
        // A completed celebration must not credit coins into the editor save when the next screen opens.
        typeof(ToolkitGameUI).GetField("rewardPending", Flags).SetValue(ui, false);
        if (s.Screen == "none") return; // time series: only waits
        if (gm.IsPaused) gm.SetPaused(false);
        switch (s.Screen)
        {
            case "start": State(gm, GameState.Start); break;
            case "settings": State(gm, GameState.Start); Call(ui, "OpenSettings"); break;
            case "reset": State(gm, GameState.Start); Call(ui, "OpenSettings"); Call(ui, "AskReset"); break;
            case "nolives": State(gm, GameState.Start); Call(ui, "ShowNoLives"); break;
            case "ready": State(gm, GameState.Ready); break;
            case "hud": State(gm, GameState.Playing); break;
            case "pause": State(gm, GameState.Playing); Call(ui, "OpenPause"); break;
            case "confirm": State(gm, GameState.Playing); Call(ui, "OpenPause"); Call(ui, "AskRestart"); break;
            case "fail":
                typeof(GameManager).GetProperty("FailReason").SetValue(gm, FailReason.NoSpace);
                State(gm, GameState.Playing); State(gm, GameState.Fail); break;
            case "complete": State(gm, GameState.Playing); State(gm, GameState.Complete); break;
            case "collect": Call(ui, "ContinueLevel"); break;
            case "retry": Call(ui, "RetryLevel"); break;
        }
    }

    // Low editor frame rates advance unscaled tweens slowly; result screens wait for their own timeline end.
    private static bool Settled(Shot s, double now)
    {
        if (now > readyAt + 40) return true;
        var ui = Object.FindObjectOfType<ToolkitGameUI>();
        float Get(string member) => (float)(typeof(ToolkitGameUI).GetField(member, Flags)?.GetValue(ui) ?? typeof(ToolkitGameUI).GetProperty(member, Flags).GetValue(ui));
        if (s.Screen == "fail") return Get("failureTime") >= Get("FailureDuration");
        if (s.Screen == "complete") return Get("celebrationTime") >= Get("IntroDuration");
        return true;
    }

    private static void Step()
    {
        try
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            Shot s = shots[index];
            double now = EditorApplication.timeSinceStartup;
            if (phase == 0)
            {
                bool same = index > 0 && shots[index - 1].W == s.W && shots[index - 1].H == s.H;
                if (!same) SetSize(s.W, s.H);
                readyAt = now + (same ? 0 : .4); phase = 1;
            }
            else if (phase == 1 && now >= readyAt)
            {
                Object.FindObjectOfType<LayoutModeController>().ApplyViewport(s.W, s.H, new Rect(0, s.Bottom, s.W, s.H - s.Top - s.Bottom));
                Show(s); readyAt = now + (s.Screen == "none" ? s.Wait : Mathf.Max(.5f, s.Wait)); phase = 2;
            }
            view.Repaint();
            if (phase != 2 || now < readyAt || !Settled(s, now)) return;
            var rt = (RenderTexture)view.GetType().GetField("m_RenderTexture", Flags).GetValue(view);
            if (rt == null || rt.width != s.W) return; // the view recreates its target after resizing
            Save(rt, folder + "/" + s.Name + ".png");
            File.WriteAllText(folder + "/" + s.Name + ".tsv", Bounds(rt.width));
            phase = 0;
            if (++index == shots.Length)
            {
                var ui = Object.FindObjectOfType<ToolkitGameUI>();
                typeof(ToolkitGameUI).GetField("rewardPending", Flags).SetValue(ui, false);
                Cleanup(); File.WriteAllText(folder + "/done.txt", "ok");
            }
        }
        catch (Exception e) { File.WriteAllText(folder + "/error.txt", e.ToString()); Cleanup(); }
    }

    // Bounds of every visible named element, in capture pixels (top-left origin).
    private static string Bounds(int w)
    {
        var sb = new StringBuilder();
        var root = Object.FindObjectOfType<UIDocument>().rootVisualElement;
        float scale = w / root.worldBound.width;
        root.Query<VisualElement>().ForEach(e =>
        {
            if (e.resolvedStyle.display == DisplayStyle.None || !e.visible) return;
            for (var p = e.parent; p != null; p = p.parent) if (p.resolvedStyle.display == DisplayStyle.None) return;
            Rect b = e.worldBound;
            var tex = e.resolvedStyle.backgroundImage.texture;
            if (tex != null && e.resolvedStyle.opacity > 0)
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "tex	{0}	{1:F0}	{2:F0}	{3}	{4}	{5}	{6}", tex.name, b.width * scale, b.height * scale,
                    tex.width, tex.height, e.resolvedStyle.unitySliceLeft, e.resolvedStyle.unitySliceTop));
            if (string.IsNullOrEmpty(e.name)) return;
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1:F0}\t{2:F0}\t{3:F0}\t{4:F0}	{5:F2}", e.name, b.x * scale, b.y * scale, b.width * scale, b.height * scale, e.resolvedStyle.opacity));
        });
        return sb.ToString();
    }

    private static void Save(RenderTexture target, string path)
    {
        var old = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply(); RenderTexture.active = old;
        File.WriteAllBytes(path, image.EncodeToPNG()); Object.DestroyImmediate(image);
    }

    public static void Cleanup()
    {
        EditorApplication.update -= Step;
        if (group != null && customIndex >= 0)
        {
            view.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(view, originalIndex);
            group.GetType().GetMethod("RemoveCustomSize", Flags).Invoke(group, new object[] { customIndex });
        }
        customIndex = -1; group = null;
    }
}
#endif
