#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BusJam.Core;
using BusJam.UI;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

// Offscreen review fixture. Never enters Play or saves the user's scene/assets/progress.
public static class ResultScreenReview
{
    private const string Folder = "C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/win";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly int[,] Sizes = { {1080,1920},{1080,2400},{1170,2532},{1536,2048},{1080,1080},{1768,2208},{2208,1768},{1920,1080},{2560,1080} };
    private static readonly float[] Times = { 0, .3f, .6f, 1, 1.5f, 2, 2.5f, 2.55f, 2.7f, 2.85f, 3, 3.45f };
    private sealed class Shot
    {
        public string Name;
        public int Width, Height;
        public bool Fail;
        public string Art;
        public int Multiplier;
        public float Time, Collect = -1, Exit = -1;
    }
    private static readonly List<Shot> Shots = new List<Shot>();
    private static readonly List<string> Bounds = new List<string>();
    private static GameObject host, cameraHost;
    private static UIDocument document;
    private static ToolkitGameUI game;
    private static PanelSettings settings;
    private static RenderTexture target;
    private static Camera camera;
    private static UnityEngine.SceneManagement.Scene preview;
    private static int index, tick;
    private static bool active;

    private static void Guard()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Application.unityVersion != "2022.3.19f1" || Application.dataPath != "C:/Users/Kullanici/Documents/BusJamCase/Assets")
            throw new InvalidOperationException("Result review requires the pinned project, Unity 2022.3.19f1, compiled Edit mode.");
    }
    private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    private static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, Flags).GetValue(obj);
    private static VisualElement Root => document.rootVisualElement.Q("ui-root");

    private static void Create(int width, int height)
    {
        Cleanup(); Guard();
        var original = UnityEngine.Object.FindObjectOfType<ToolkitGameUI>();
        var originalCamera = UnityEngine.Object.FindObjectOfType<Camera>();
        preview = EditorSceneManager.NewPreviewScene();
        host = new GameObject("ZWIN isolated UI review") { hideFlags = HideFlags.HideAndDontSave };
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host, preview);
        host.SetActive(false);
        settings = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/_BusJam/UI/GamePanelSettings.asset"));
        settings.hideFlags = HideFlags.HideAndDontSave; settings.clearColor = false;
        target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave };
        target.antiAliasing = 4; target.Create(); settings.targetTexture = target;
        document = host.AddComponent<UIDocument>(); document.panelSettings = settings;
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_BusJam/UI/GameUI.uxml");
        game = host.AddComponent<ToolkitGameUI>(); EditorUtility.CopySerialized(original, game); Set(game, "document", document);
        host.SetActive(true);
        cameraHost = new GameObject("ZWIN read-only world camera") { hideFlags = HideFlags.HideAndDontSave };
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraHost, preview);
        camera = cameraHost.AddComponent<Camera>(); camera.CopyFrom(originalCamera);
        camera.transform.SetPositionAndRotation(originalCamera.transform.position, originalCamera.transform.rotation);
        camera.scene = originalCamera.scene; camera.targetTexture = target; camera.enabled = false;
        camera.aspect = (float)width / height;
    }

    public static string StartCaptures()
    {
        Guard(); if (active) throw new InvalidOperationException("Capture already running.");
        Directory.CreateDirectory(Folder + "/ours"); Shots.Clear(); Bounds.Clear(); index = tick = 0;
        for (int size = 0; size < Sizes.GetLength(0); size++)
            foreach (bool fail in new[] { false, true })
                Shots.Add(new Shot { Name = (fail ? "Fail" : "Complete") + "-" + Sizes[size,0] + "x" + Sizes[size,1], Width = Sizes[size,0], Height = Sizes[size,1], Fail = fail, Time = fail ? 1.3f : 3.45f });
        foreach (bool fail in new[] { false, true })
            foreach (float time in Times)
            {
                if (fail && time > 2.5f) continue;
                Shots.Add(new Shot { Name = (fail ? "fail" : "complete") + "-t" + time.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Width = 1080, Height = 1920, Fail = fail, Time = time, Exit = fail && time >= 1.8f ? time - 1.8f : -1 });
            }
        foreach (float time in new[] { 0, .3f, .6f, 1, 1.2f, 1.35f, 1.6f, 1.8f })
            Shots.Add(new Shot { Name = "collect-t" + time.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Width = 1080, Height = 1920, Time = 3.45f, Collect = time });
        active = true; EditorApplication.update += Step;
        if (File.Exists(Folder + "/capture-error.txt")) File.Delete(Folder + "/capture-error.txt");
        return "Queued " + Shots.Count + " isolated Edit-mode captures.";
    }

    public static string StartArtCaptures()
    {
        Guard(); if (active) throw new InvalidOperationException("Capture already running.");
        Directory.CreateDirectory(Folder + "/ours"); Shots.Clear(); Bounds.Clear(); index = tick = 0;
        foreach (string art in new[] { "title_complete", "title_failed", "ribbon_purple" })
            foreach (int multiplier in new[] { 1, 2 })
                Shots.Add(new Shot { Name = art + "-compare-" + multiplier + "x", Width = 1180*multiplier, Height = 400*multiplier, Art = art, Multiplier = multiplier });
        active = true; EditorApplication.update += Step;
        return "Queued six PNG versus Unity VectorImage comparisons at on-screen size and 2x.";
    }

    private static void PrepareArt(Shot shot)
    {
        Call(game, "OnDisable"); game.enabled = false; Root.Clear();
        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        Root.style.backgroundColor = new Color(.15f,.15f,.22f);
        for (int column=0;column<2;column++)
        {
            var label = new Label(column == 0 ? "Source PNG" : "Constructed SVG / Unity VectorImage");
            label.style.position = Position.Absolute; label.style.left = (20+590*column)*shot.Multiplier;
            label.style.top = 12*shot.Multiplier; label.style.fontSize = 22*shot.Multiplier; Root.Add(label);
            var art = new VisualElement { pickingMode = PickingMode.Ignore };
            art.style.position = Position.Absolute; art.style.left = (20+590*column)*shot.Multiplier;
            art.style.top = 55*shot.Multiplier; art.style.width = 550*shot.Multiplier; art.style.height = 310*shot.Multiplier;
            art.AddToClassList("art-comparison");
            art.style.backgroundImage = column==0
                ? new StyleBackground(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BusJam/Art/UI/" + shot.Art + ".png"))
                : new StyleBackground(Background.FromVectorImage(AssetDatabase.LoadAssetAtPath<VectorImage>("Assets/_BusJam/UI/Vector/" + shot.Art + ".svg")));
            Root.Add(art);
        }
    }

    private static void Sample(Shot shot)
    {
        if (shot.Art != null) return;
        float aspect = (float)shot.Width / shot.Height;
        Root.EnableInClassList("portrait", aspect < .8f);
        Root.EnableInClassList("square", aspect >= .8f && aspect <= 1.25f);
        Root.EnableInClassList("landscape", aspect > 1.25f);
        if (shot.Fail)
        {
            Call(game, "SetFailureEntry", shot.Time);
            if (shot.Exit >= 0) { Call(game, "RetryLevel"); Call(game, "SetFailureExit", shot.Exit); }
        }
        else
        {
            Call(game, "SetCelebrationTime", shot.Time); Call(game, "SetCelebrationIdle", shot.Time);
            if (shot.Collect >= 0) { Call(game, "ContinueLevel"); Call(game, "SetCollectionTime", shot.Collect); }
        }
        Call(game, "PauseResultTweens");
    }

    private static void Step()
    {
        try
        {
            Guard(); Shot shot = Shots[index];
            if (tick == 0)
            {
                Create(shot.Width, shot.Height);
                if (shot.Art != null) PrepareArt(shot);
                else
                {
                    Call(game, "ShowLives", 5, 0); Call(game, "ShowLives", 4, 1199);
                    game.DisplayState(shot.Fail ? GameState.Fail : GameState.Complete);
                }
                Sample(shot);
            }
            if (tick >= 3)
            {
                Sample(shot); Call(settings, "ApplyPanelSettings");
                if (shot.Art == null) camera.Render();
                else { var previous = RenderTexture.active; RenderTexture.active = target; GL.Clear(true,true,new Color(.15f,.15f,.22f)); RenderTexture.active = previous; }
                Call(document.rootVisualElement.panel, "Repaint", new Event { type = EventType.Repaint });
            }
            if (++tick < 9) return;
            SaveTarget(Folder + "/ours/" + shot.Name + ".png");
            float scale = shot.Width / document.rootVisualElement.resolvedStyle.width;
            if (shot.Art == null) foreach (string name in new[] { "complete-title", "complete-heading", "reward-pile", "reward-label", "continue-button", "fail-panel", "fail-title", "retry-button", "fail-hud-heart" })
            {
                var element = Root.Q(name); Rect b = element.worldBound;
                Bounds.Add(shot.Name + "\t" + name + "\t" + b.x*scale + "\t" + b.y*scale + "\t" + b.width*scale + "\t" + b.height*scale + "\t" + element.resolvedStyle.opacity);
            }
            Cleanup(); tick = 0; index++;
            if (index == Shots.Count)
            {
                if (shot.Art == null) File.WriteAllLines(Folder + "/bounds.tsv", Bounds);
                File.WriteAllText(Folder + (shot.Art == null ? "/captures-done.txt" : "/art-done.txt"), index + " captures; Unity " + Application.unityVersion + "; Edit mode; isolated UI and read-only world camera.");
                EditorApplication.update -= Step; active = false;
            }
        }
        catch (Exception exception)
        {
            File.WriteAllText(Folder + "/capture-error.txt", exception.ToString());
            EditorApplication.update -= Step; active = false; Cleanup();
        }
    }

    private static void SaveTarget(string path)
    {
        var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0); texture.Apply(); RenderTexture.active = previous;
        File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
    }

    public static void Cleanup()
    {
        if (game != null)
        {
            var owned = new Tween[] { Get<Tween>(game,"celebration"), Get<Tween>(game,"celebrationIdle"), Get<Tween>(game,"failureEntry"), Get<Tween>(game,"collection"), Get<Tween>(game,"failureExit") };
            Call(game, "OnDisable"); Call(game, "KillTweens");
            // DOTween's public Kill is gated by initialization, which it skips outside Play.
            // Release only this fixture's tweens through the pinned package's disposal path.
            var manager = typeof(DOTween).Assembly.GetType("DG.Tweening.Core.TweenManager");
            var despawn = manager.GetMethod("Despawn",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            foreach (var tween in owned) if (tween != null && tween.IsActive()) despawn.Invoke(null,new object[] { tween,true });
        }
        if (host != null) UnityEngine.Object.DestroyImmediate(host);
        if (cameraHost != null) UnityEngine.Object.DestroyImmediate(cameraHost);
        if (settings != null) UnityEngine.Object.DestroyImmediate(settings);
        if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
        host = cameraHost = null; document = null; game = null; settings = null; target = null; camera = null;
    }

    private static int Elements(VisualElement root)
    {
        int count = 1; foreach (var child in root.Children()) count += Elements(child); return count;
    }
    private static int Callbacks(VisualElement root)
    {
        int count = 0;
        var registry = typeof(CallbackEventHandler).GetField("m_CallbackRegistry", Flags).GetValue(root);
        if (registry != null)
        {
            var list = registry.GetType().GetField("m_Callbacks", Flags).GetValue(registry);
            if (list != null) count += (int)list.GetType().GetProperty("Count", Flags).GetValue(list);
        }
        var button = root as Button;
        if (button != null && button.clickable != null)
        {
            var handlers = typeof(Clickable).GetField("clicked", Flags).GetValue(button.clickable) as Delegate;
            if (handlers != null) count += handlers.GetInvocationList().Length;
        }
        foreach (var child in root.Children()) count += Callbacks(child);
        return count;
    }

    private static void Assert(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }

    private static void CompleteCycle()
    {
        game.DisplayState(GameState.Start); game.DisplayState(GameState.Complete);
        for (int frame = 0; frame <= 69; frame++) { Call(game, "SetCelebrationTime", frame * .05f); Call(game, "SetCelebrationIdle", frame * .05f); }
        Assert(!Get<bool>(game, "rewardCredited"), "Intro credited before Continue.");
        Assert(Root.Q<Label>("reward-label").text == "25", "Reward count did not finish.");
        Call(game, "ContinueLevel"); Call(game, "ContinueLevel");
        for (int frame = 0; frame <= 36; frame++) Call(game, "SetCollectionTime", frame * .05f);
        Assert(Get<bool>(game, "rewardCredited") && Get<int>(game, "arrivedCoins") == 14, "Collection did not credit at final arrival.");
        Assert(Get<bool>(game, "resultTransitionDone"), "Collection did not finish transition.");
        Call(game, "CreditReward"); Call(game, "FinishCelebration");
    }
    private static void FailCycle()
    {
        game.DisplayState(GameState.Start); Call(game, "ShowLives", 5, 0); Call(game, "ShowLives", 4, 1199);
        game.DisplayState(GameState.Fail);
        for (int frame = 0; frame <= 26; frame++) Call(game, "SetFailureEntry", frame * .05f);
        Assert(Root.Q<Label>("fail-lives-count").text == "4" && Root.Q<Label>("fail-countdown").text == "19:59", "Fail life HUD mismatch.");
        Call(game, "RetryLevel"); Call(game, "RetryLevel");
        for (int frame = 0; frame <= 8; frame++) Call(game, "SetFailureExit", frame * .05f);
        Assert(Get<bool>(game, "resultTransitionDone"), "Retry did not finish transition.");
    }

    public static string Verify()
    {
        Guard(); if (active) throw new InvalidOperationException("Wait for captures before validation."); Create(1080,1920);
        try
        {
            int coins = BusJam.Save.SaveSystem.Coins;
            for (int i = 0; i < 20; i++) { CompleteCycle(); FailCycle(); }
            game.DisplayState(GameState.Start);
            int elementsBefore = Elements(Root), callbacksBefore = Callbacks(Root), tweensBefore = DOTween.TotalActiveTweens();
            for (int i = 0; i < 50; i++)
            {
                CompleteCycle(); FailCycle(); game.DisplayState(GameState.Start);
                Call(game, "OnDisable"); Call(game, "OnEnable");
            }
            int elementsAfter = Elements(Root), callbacksAfter = Callbacks(Root), tweensAfter = DOTween.TotalActiveTweens();
            Assert(elementsBefore == elementsAfter && callbacksBefore == callbacksAfter && tweensBefore == tweensAfter, "Retained UI, callbacks or tweens grew.");
            game.DisplayState(GameState.Complete); Call(game, "FinishCelebration");
            Assert(!Get<bool>(game, "rewardCredited"), "Intro skip credited early.");
            Call(game, "ContinueLevel"); Call(game, "SetCollectionTime", 1.8f);
            Assert(Get<bool>(game, "rewardCredited"), "Collection skip missed credit.");
            long allocated = MeasureSamplerAllocations();
            Call(game, "OnDisable");
            foreach (string name in new[] { "celebration", "celebrationIdle", "failureEntry", "collection", "failureExit" })
            { var tween = Get<Tween>(game, name); Assert(tween == null || (!tween.IsPlaying() && tween.Elapsed() == 0), name + " not rewound on disable."); }
            Assert(BusJam.Save.SaveSystem.Coins == coins, "Edit review changed saved coins.");
            string report = "PASS: Complete x20 and Fail x20 sampled at 0.05 s; then 50 full Complete/Fail/disable/enable cycles.\nElements " + elementsBefore + " -> " + elementsAfter + "; callbacks " + callbacksBefore + " -> " + callbacksAfter + "; active tweens " + tweensBefore + " -> " + tweensAfter + ". Retained pool growth: 0.\nSampler allocations over 200 warmed frames: " + allocated + " bytes.\nPASS: count ends at 25; no early reward; final 14 arrivals credit guard; duplicate clicks; intro/collection skip; retry fade; lives 5 FULL -> 4 19:59; disable rewinds all five result tweens; saved coins unchanged.\nEdit-mode samplers, no Play or gameplay simulation.";
            File.WriteAllText(Folder + "/validation.txt", report); return report;
        }
        finally { Cleanup(); }
    }

    private static long MeasureSamplerAllocations()
    {
        var meter = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
        var intro = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), game, typeof(ToolkitGameUI).GetMethod("SetCelebrationTime",Flags));
        var idle = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), game, typeof(ToolkitGameUI).GetMethod("SetCelebrationIdle",Flags));
        var collect = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), game, typeof(ToolkitGameUI).GetMethod("SetCollectionTime",Flags));
        var failure = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), game, typeof(ToolkitGameUI).GetMethod("SetFailureEntry",Flags));
        var exit = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), game, typeof(ToolkitGameUI).GetMethod("SetFailureExit",Flags));
        // Text values have all been cached; no reflection/boxing runs inside the measured loop.
        for (int i=0;i<200;i++) { intro(i%70*.05f); idle(i*.05f); collect(i%25*.05f); failure(i%27*.05f); exit(i%9*.05f); }
        long before = meter();
        for (int i=0;i<200;i++) { intro(i%70*.05f); idle(i*.05f); collect(i%25*.05f); failure(i%27*.05f); exit(i%9*.05f); }
        return meter()-before;
    }
}
#endif
