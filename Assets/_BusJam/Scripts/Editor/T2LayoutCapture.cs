#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using BusJam.Core;
using BusJam.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>T2 review: Play mode Game View captures at fixed sizes with simulated safe areas; writes PNG + element bounds.</summary>
public static class T2LayoutCapture
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private struct Shot { public int W, H, Top, Bottom, Level; public string Name; }
    private static Shot[] shots;
    private static string folder;
    private static EditorWindow view;
    private static object group;
    private static int originalIndex, customIndex = -1, index, tick;

    /// <summary>spec: "name,w,h,safeTop,safeBottom,levelIndex;..." (safe insets in pixels).</summary>
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
            shots[i] = new Shot { Name = p[0], W = int.Parse(p[1]), H = int.Parse(p[2]), Top = int.Parse(p[3]), Bottom = int.Parse(p[4]), Level = int.Parse(p[5]) };
        }
        var gvType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        view = EditorWindow.GetWindow(gvType);
        originalIndex = (int)gvType.GetProperty("selectedSizeIndex", Flags).GetValue(view);
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(Editor).Assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
        group = sizesType.GetProperty("currentGroup", Flags).GetValue(singleton.GetProperty("instance", Flags).GetValue(null));
        index = tick = 0;
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
        group.GetType().GetMethod("AddCustomSize", Flags).Invoke(group, new[] { Activator.CreateInstance(sizeType, Flags, null, new object[] { Enum.Parse(enumType, "FixedResolution"), w, h, "T2" }, null) });
        view.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(view, customIndex);
    }

    private static void Step()
    {
        try
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            Shot s = shots[index];
            var gm = Object.FindObjectOfType<GameManager>();
            if (tick == 0) SetSize(s.W, s.H);
            if (tick == 8)
            {
                Object.FindObjectOfType<LayoutModeController>().ApplyViewport(s.W, s.H, new Rect(0, s.Bottom, s.W, s.H - s.Top - s.Bottom));
                typeof(GameManager).GetField("currentLevelIndex", Flags).SetValue(gm, s.Level);
                typeof(GameManager).GetMethod("LoadLevel", Flags).Invoke(gm, new object[] { s.Level });
                typeof(GameManager).GetMethod("SetState", Flags).Invoke(gm, new object[] { GameState.Playing });
            }
            view.Repaint();
            if (++tick < 40) return;
            var rt = (RenderTexture)view.GetType().GetField("m_RenderTexture", Flags).GetValue(view);
            Save(rt, folder + "/" + s.Name + ".png");
            File.WriteAllText(folder + "/" + s.Name + ".tsv", Bounds(rt.width, rt.height));
            tick = 0;
            if (++index == shots.Length) { Cleanup(); File.WriteAllText(folder + "/done.txt", "ok"); }
        }
        catch (Exception e) { File.WriteAllText(folder + "/error.txt", e.ToString()); Cleanup(); }
    }

    // UI bounds in capture pixels (top-left origin) plus projected slot/grid anchors.
    private static string Bounds(int w, int h)
    {
        var sb = new StringBuilder();
        var doc = Object.FindObjectOfType<UIDocument>();
        var root = doc.rootVisualElement;
        float scale = w / root.worldBound.width;
        foreach (var name in new[] { "safe-area", "hud", "level-badge", "pause-settings", "booster-bar", "ad-banner", "timer-counter" })
        {
            var e = root.Q(name) ?? root.Q(className: name);
            if (e == null) continue;
            Rect b = e.worldBound;
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1:F0}\t{2:F0}\t{3:F0}\t{4:F0}", name, b.x * scale, b.y * scale, b.width * scale, b.height * scale));
        }
        var cam = Camera.main; var area = Object.FindObjectOfType<WaitingArea>();
        for (int i = 0; i < GameRules.VisibleSlotCount; i += GameRules.VisibleSlotCount - 1)
        {
            Vector3 p = cam.WorldToScreenPoint(area.SlotPosition(i));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "slot{0}\t{1:F0}\t{2:F0}", i, p.x * w / cam.pixelWidth, h - p.y * h / cam.pixelHeight));
        }
        var board = Object.FindObjectOfType<BoardView>();
        var fitter = cam.GetComponent<CameraFitter>();
        var roof = (Vector3)typeof(CameraFitter).GetField("busRoofLocal", Flags).GetValue(fitter);
        foreach (var local in new[] { roof, new Vector3(0, 0, .5f), new Vector3(0, 0, -7.5f) })
        {
            Vector3 v = cam.WorldToViewportPoint(board.transform.TransformPoint(local));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "anchor{0}	{1:F0}", local, (1 - v.y) * ScreenLayout.ReferenceHeight));
        }
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "camera\t{0}\t{1}", cam.transform.position, cam.fieldOfView));
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
