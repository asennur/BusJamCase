#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BusJam.Core;
using BusJam.Level;
using BusJam.UI;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Isolated Edit-mode evidence; never saves a scene or writes player progress.</summary>
public static class CelebrationReview
{
    private const string Folder = "C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/celeb3";
    private const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static readonly int[,] Sizes = {{1080,1920},{1080,2400},{1170,2532},{1536,2048},{1080,1080},{1768,2208},{2208,1768},{1920,1080},{2560,1080}};
    private static ToolkitGameUI game;
    private static CompleteCelebration bus;
    private static UIDocument document;
    private static PanelSettings settings;
    private static Camera worldCamera;
    private static RenderTexture target;
    private static LevelData level;
    private static int index, tick;
    private static bool running;
    private sealed class Shot { public int Width, Height; public float Time; public string Name; }
    private static readonly List<Shot> Shots = new List<Shot>();
    private static readonly List<string> Measurements = new List<string>();
    private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
    private static object Review(string name, params object[] args) => typeof(ResultScreenReview).GetMethod(name, Flags).Invoke(null, args);
    private static T Field<T>(string name) => (T)typeof(ResultScreenReview).GetField(name, Flags).GetValue(null);
    private static VisualElement Root => document.rootVisualElement.Q("ui-root");

    private static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Application.unityVersion != "2022.3.19f1" || Application.dataPath != "C:/Users/Kullanici/Documents/BusJamCase/Assets")
            throw new InvalidOperationException("Pinned project, Unity 2022.3.19f1 and compiled Edit mode required.");
    }

    private static void Create(int width, int height)
    {
        Guard(); Review("Create", width, height);
        game = Field<ToolkitGameUI>("game"); document = Field<UIDocument>("document");
        target = Field<RenderTexture>("target"); settings = Field<PanelSettings>("settings");
        worldCamera = Field<Camera>("camera");
        Call(game, "InitializeCelebrationBus");
        bus = (CompleteCelebration)typeof(ToolkitGameUI).GetField("celebrationBus", Flags).GetValue(game);
        bus.Camera.scene = game.gameObject.scene; bus.Resize(width, height);
        level = ScriptableObject.CreateInstance<LevelData>();
        level.busOrder.Add(PassengerColor.Red); level.busOrder.Add(PassengerColor.Purple);
        game.DisplayState(GameState.Complete); bus.Prepare(level);
        Root.EnableInClassList("portrait", (float)width/height < .8f);
        Root.EnableInClassList("square", (float)width/height >= .8f && (float)width/height <= 1.25f);
        Root.EnableInClassList("landscape", (float)width/height > 1.25f);
    }

    private static void Sample(float time)
    {
        Call(game, "SetCelebrationTime", time); Call(game, "SetCelebrationIdle", time);
        Call(game, "PauseResultTweens");
    }

    private static void Render()
    {
        Call(settings, "ApplyPanelSettings");
        worldCamera.Render();
        if (bus.Visible) bus.Camera.Render();
        Call(document.rootVisualElement.panel, "Repaint", new Event { type = EventType.Repaint });
    }

    public static string StartCaptures(bool quick = false)
    {
        Guard(); if (running) throw new InvalidOperationException("Already capturing.");
        Directory.CreateDirectory(Folder + "/ours"); Directory.CreateDirectory(Folder + "/frames");
        Shots.Clear(); Measurements.Clear(); index = tick = 0;
        Measurements.Add("shot\tresolution\toffset_s\trail_top_px\twheel_bottom_px\trail_to_wheel_percent\trider\tfoot_above_deck_world\tfoot_body_ratio\tfoot_screen_y_px\tdeck_screen_y_px\thead_top_px\ttitle_bottom_px");
        foreach (float offset in new[] {0, .3f, .6f, .9f, 1.2f})
            Shots.Add(new Shot {Width=1080,Height=1920,Time=1.05f+offset,Name="ours/timeline-"+offset.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)});
        if (!quick)
        {
            for (int i=0;i<Sizes.GetLength(0);i++)
                foreach (bool mid in new[] {true,false}) Shots.Add(new Shot {Width=Sizes[i,0],Height=Sizes[i,1],Time=mid?1.725f:3.45f,
                    Name="ours/"+(mid?"mid-":"end-")+Sizes[i,0]+"x"+Sizes[i,1]});
            for (int i=0;i<Sizes.GetLength(0);i++) Shots.Add(new Shot {Width=Sizes[i,0],Height=Sizes[i,1],Time=1.95f,
                Name="ours/overlap-"+Sizes[i,0]+"x"+Sizes[i,1]});
            for (int frame=0;frame<=105;frame++) Shots.Add(new Shot {Width=1080,Height=1920,Time=frame/30f,Name="frames/frame-"+frame.ToString("D3")});
        }
        running = true; EditorApplication.update += Step;
        return "Queued " + Shots.Count + " Edit-mode captures.";
    }

    private static void Step()
    {
        try
        {
            Guard(); Shot shot = Shots[index];
            if (tick == 0)
            {
                if (game == null || target.width != shot.Width || target.height != shot.Height)
                { Cleanup(); Create(shot.Width,shot.Height); }
                Sample(shot.Time);
            }
            if (tick >= 3) { Sample(shot.Time); Render(); }
            if (++tick < 6) return;
            Review("SaveTarget", Folder + "/" + shot.Name + ".png");
            if (bus.Visible && shot.Name.StartsWith("ours/", StringComparison.Ordinal)) MeasurePose(shot);
            if (shot.Name.StartsWith("ours/mid-", StringComparison.Ordinal)) SaveBusMask(shot.Name.Substring(9));
            tick = 0; index++;
            if (index == Shots.Count)
            {
                Stop(); File.WriteAllText(Folder+"/captures-done.txt",index+" captures, Unity "+Application.unityVersion+", Edit mode only.");
                File.WriteAllLines(Folder+"/pose-measurements.tsv",Measurements);
            }
        }
        catch (Exception error) { File.WriteAllText(Folder+"/capture-error.txt",error.ToString()); Stop(); }
    }

    private static T BusField<T>(string name) => (T)typeof(CompleteCelebration).GetField(name, Flags).GetValue(bus);
    private static Vector3[] SkinVertices(SkinnedMeshRenderer body)
    {
        var mesh = new Mesh();
        try
        {
            body.BakeMesh(mesh, true);
            var points = mesh.vertices;
            for (int i=0;i<points.Length;i++) points[i]=body.transform.TransformPoint(points[i]);
            return points;
        }
        finally { UnityEngine.Object.DestroyImmediate(mesh); }
    }

    private static void MeasurePose(Shot shot)
    {
        var body=BusField<Renderer>("busBody") as SkinnedMeshRenderer;
        Vector3[] vertices=SkinVertices(body);
        float railTop=0, wheelBottom=shot.Height;
        foreach(int i in body.sharedMesh.GetIndices(9)) railTop=Mathf.Max(railTop,bus.Camera.WorldToScreenPoint(vertices[i]).y);
        foreach(int i in body.sharedMesh.GetIndices(6)) wheelBottom=Mathf.Min(wheelBottom,bus.Camera.WorldToScreenPoint(vertices[i]).y);
        var feet=BusField<Transform[]>("feet"); var soles=BusField<Vector3[]>("footSoles");
        var decks=BusField<Vector3[]>("deckPositions"); var heights=BusField<float[]>("riderHeights");
        var vehicle=BusField<Transform>("bus"); var riders=BusField<Renderer[]>("riderBodies");
        var heads=BusField<Transform[]>("heads");
        var cast=BusField<GameObject>("cast");
        float uiScale=shot.Width/document.rootVisualElement.resolvedStyle.width;
        float titleBottom=Root.Q("complete-heading").worldBound.yMax*uiScale;
        for(int i=0;i<3;i++)
        {
            Vector3 sole=feet[i].TransformPoint(soles[i]), deck=vehicle.TransformPoint(decks[i]);
            float lift=Vector3.Dot(sole-deck,vehicle.up), headTop=0;
            var skin=(SkinnedMeshRenderer)riders[i];
            var points=SkinVertices(skin); var weights=skin.sharedMesh.boneWeights;
            int head=Array.IndexOf(skin.bones,heads[i]);
            for(int v=0;v<points.Length;v++) if(weights[v].boneIndex0==head && weights[v].weight0>.5f)
                headTop=Mathf.Max(headTop,bus.Camera.WorldToScreenPoint(points[v]).y);
            string[] values={shot.Name,shot.Width+"x"+shot.Height,(shot.Time-CompleteCelebration.StartTime).ToString("F3"),
                (shot.Height-railTop).ToString("F2"),(shot.Height-wheelBottom).ToString("F2"),((railTop-wheelBottom)/shot.Height*100).ToString("F3"),
                i.ToString(),lift.ToString("F5"),(lift/(heights[i]*cast.transform.localScale.y)).ToString("F5"),
                (shot.Height-bus.Camera.WorldToScreenPoint(sole).y).ToString("F2"),(shot.Height-bus.Camera.WorldToScreenPoint(deck).y).ToString("F2"),
                (shot.Height-headTop).ToString("F2"),titleBottom.ToString("F2")};
            Measurements.Add(string.Join("\t",values));
        }
    }

    public static string VerifyMotion()
    {
        Guard(); Assert(!running,"Wait for captures."); Create(1080,1920);
        try
        {
            var feet=BusField<Transform[]>("feet"); var soles=BusField<Vector3[]>("footSoles");
            var decks=BusField<Vector3[]>("deckPositions"); var heights=BusField<float[]>("riderHeights");
            var vehicle=BusField<Transform>("bus"); var cast=BusField<GameObject>("cast");
            int[] grounded=new int[3]; float[] peaks=new float[3]; int maxAir=0;
            for(int frame=0;frame<600;frame++)
            {
                bus.Sample(CompleteCelebration.StartTime+frame*.001f); int air=0;
                for(int i=0;i<3;i++)
                {
                    float lift=Vector3.Dot(feet[i].TransformPoint(soles[i])-vehicle.TransformPoint(decks[i]),vehicle.up);
                    float ratio=lift/(heights[i]*cast.transform.localScale.y);
                    peaks[i]=Mathf.Max(peaks[i],ratio);
                    if(ratio<=.001f) grounded[i]++; else air++;
                }
                maxAir=Mathf.Max(maxAir,air);
            }
            Assert(maxAir<=2,"Three riders airborne together.");
            for(int i=0;i<3;i++) Assert(grounded[i]>=360 && peaks[i]<=.501f,"Grounded fraction or hop peak out of range.");
            string report="PASS: 600 samples over a full 0.6 s cycle. Max simultaneous airborne riders: "+maxAir+".\n";
            for(int i=0;i<3;i++) report+="Rider "+i+": grounded "+(grounded[i]/6f).ToString("F2")+"%; peak "+peaks[i].ToString("F4")+" x cached standing body height.\n";
            File.WriteAllText(Folder+"/motion-validation.txt",report); return report;
        }
        finally { Cleanup(); }
    }

    private static void SaveBusMask(string size)
    {
        var bodies = (Renderer[])typeof(CompleteCelebration).GetField("riderBodies", Flags).GetValue(bus);
        foreach (var body in bodies) body.enabled = false;
        var previous = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            bus.Camera.Render(); RenderTexture.active = bus.Target;
            texture = new Texture2D(bus.Target.width, bus.Target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); texture.Apply();
            File.WriteAllBytes(Folder + "/ours/bus-mask-" + size + ".png", texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            foreach (var body in bodies) body.enabled = true;
        }
    }

    public static void Stop() { EditorApplication.update -= Step; running = false; Cleanup(); }
    private static void Cleanup()
    {
        Review("Cleanup"); if(level != null) UnityEngine.Object.DestroyImmediate(level);
        game = null; bus = null; document = null; target = null; settings = null; worldCamera = null; level = null;
    }

    private static void Assert(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static int Elements(VisualElement element)
    { int count=1; foreach(var child in element.Children()) count+=Elements(child); return count; }

    private static void Cycle()
    {
        game.DisplayState(GameState.Start); game.DisplayState(GameState.Complete); bus.Prepare(level);
        for(int frame=0;frame<=69;frame++) Sample(frame*.05f);
        Assert(!bus.Visible && !bus.Camera.enabled,"Camera stayed active after exit.");
        Sample(1.7f); Assert(bus.Visible,"Mid-pass not active.");
        Call(game,"FinishCelebration"); Assert(!bus.Visible,"Skip did not hide cast.");
        game.DisplayState(GameState.Start);
    }

    public static string Verify()
    {
        Guard(); Assert(!running,"Wait for captures."); Create(1080,1920);
        try
        {
            int savedCoins=BusJam.Save.SaveSystem.Coins;
            var sample=(Action<float>)Delegate.CreateDelegate(typeof(Action<float>),game,typeof(ToolkitGameUI).GetMethod("SetCelebrationTime",Flags));
            for(int i=0;i<20;i++) Cycle();
            int objectsBefore=bus.Stage.GetComponentsInChildren<Transform>(true).Length;
            int elementsBefore=Elements(Root), tweensBefore=DOTween.TotalActiveTweens();
            int texturesBefore=OwnedTargets(), materialsBefore=Resources.FindObjectsOfTypeAll<Material>().Length;
            int targetId=bus.Target.GetInstanceID();
            for(int i=0;i<50;i++)
            {
                Cycle(); Call(game,"OnDisable"); Assert(!bus.Visible,"Disable left camera active."); Call(game,"OnEnable");
            }
            int objectsAfter=bus.Stage.GetComponentsInChildren<Transform>(true).Length;
            int elementsAfter=Elements(Root), tweensAfter=DOTween.TotalActiveTweens();
            int texturesAfter=OwnedTargets(), materialsAfter=Resources.FindObjectsOfTypeAll<Material>().Length;
            Assert(objectsBefore==objectsAfter && elementsBefore==elementsAfter && tweensBefore==tweensAfter,"Pool growth.");
            Assert(texturesBefore==texturesAfter && materialsBefore==materialsAfter,"Texture or material growth.");
            Assert(targetId==bus.Target.GetInstanceID(),"RenderTexture replaced without resolution change.");
            for(int i=0;i<200;i++) sample(1.2f+i%21*.05f);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<200;i++) sample(1.2f+i%21*.05f);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert(allocated==0,"Celebration sampler allocated "+allocated+" bytes.");
            game.DisplayState(GameState.Start); game.DisplayState(GameState.Complete); Sample(1.7f);
            using(var down=PointerDownEvent.GetPooled()) Call(game,"SkipCelebration",down);
            Assert(!bus.Visible,"Pointer skip retained bus.");
            using(var up=PointerUpEvent.GetPooled()) Call(game,"ReleaseSkipPointer",up);
            foreach(PassengerColor color in Enum.GetValues(typeof(PassengerColor)))
            {
                level.busOrder[1]=color; bus.Prepare(level); sample(1.7f);
                var expected=Resources.Load<CelebrationSettings>("CelebrationSettings").colors[(int)color];
                var body=(Renderer)typeof(CompleteCelebration).GetField("busBody",Flags).GetValue(bus);
                Assert(body.sharedMaterials[0]==expected,"Last bus material mismatch.");
                foreach(var rider in (Renderer[])typeof(CompleteCelebration).GetField("riderBodies",Flags).GetValue(bus))
                    Assert(rider.sharedMaterial==expected,"Passenger material mismatch.");
            }
            RenderTexture old=bus.Target; bus.Resize(1920,1080); Assert(old==null,"Old RT not destroyed on resize.");
            Assert(bus.Target.width==1920 && bus.Target.height==1080,"Resize dimensions.");
            sample(1.7f); game.DisplayState(GameState.Fail); Assert(!bus.Visible,"Fail retained celebration.");
            sample(1.7f); Call(game,"OnDisable"); Assert(!bus.Visible,"Mid-pass disable retained camera.");
            Assert(BusJam.Save.SaveSystem.Coins==savedCoins,"Saved coins changed.");
            string report="PASS: 20 Complete runs, then 50 Complete/disable/enable cycles.\nPool transforms "+objectsBefore+" -> "+objectsAfter+
                "; UI elements "+elementsBefore+" -> "+elementsAfter+"; active DOTween handles "+tweensBefore+" -> "+tweensAfter+
                ". Zero retained growth; same RenderTexture identity.\n200 warmed active sampler frames: "+allocated+
                " managed bytes.\nOwned render targets "+texturesBefore+" -> "+texturesAfter+"; loaded shared materials "+materialsBefore+" -> "+materialsAfter+
                ".\nPASS: all seven last-bus colours and three matching riders; pointer skip/release, mid-pass disable, Fail transition; old RT released/destroyed on resize; saved coins unchanged.\nEdit-mode deterministic production samplers; no Play or gameplay simulation.";
            File.WriteAllText(Folder+"/validation.txt",report); return report;
        }
        finally { Cleanup(); }
    }

    private static int OwnedTargets()
    {
        int count=0;
        foreach(var texture in Resources.FindObjectsOfTypeAll<RenderTexture>()) if(texture.name=="Celebration Screen Target") count++;
        return count;
    }
}
#endif
