#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BusJam.Core;
using BusJam.Level;
using BusJam.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Native Game View evidence and deterministic Edit-mode checks. Never saves a scene.</summary>
public static class ZScnReview
{
    public const string Folder = "C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-1";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Vector2Int[] Sizes = { new Vector2Int(580,1250), new Vector2Int(1080,1920), new Vector2Int(2560,1080) };
    private sealed class Shot { public int Size; public float Time; public string Name; }
    private static readonly List<Shot> shots = new List<Shot>();
    private static ToolkitGameUI game;
    private static UIDocument document;
    private static CompleteCelebration bus;
    private static ResultTimeline timeline;
    private static EditorWindow view;
    private static object group;
    private static int originalIndex, initialCount, index, tick;
    private static bool running;
    private static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Flags).Invoke(value, args);
    private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Flags).GetValue(value);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Guard()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && Application.unityVersion == "2022.3.19f1"
            && Application.dataPath == "C:/Users/Kullanici/Documents/BusJamCase/Assets", "Pinned compiled Edit mode required.");
    }

    public static string Start(bool video = false)
    {
        Guard(); Check(!running, "Capture already running."); Directory.CreateDirectory(Folder);
        timeline = AssetDatabase.LoadAssetAtPath<ResultTimeline>("Assets/_BusJam/Data/ResultTimeline.asset");
        string mode = "complete";
        File.WriteAllText(Folder + "/" + mode + "-asset.json", EditorJsonUtility.ToJson(timeline, true));
        game = Object.FindObjectOfType<ToolkitGameUI>(); document = game.GetComponent<UIDocument>();
        Check(document.panelSettings.targetTexture == null, "Evidence requires the screen panel, not an offscreen UI target.");
        Call(game, "InitializeCelebrationBus"); bus = Field<CompleteCelebration>(game, "celebrationBus");
        view = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        originalIndex = (int)view.GetType().GetProperty("selectedSizeIndex",Flags).GetValue(view);
        var sizesType=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(Editor).Assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
        group=sizesType.GetProperty("currentGroup",Flags).GetValue(singleton.GetProperty("instance",Flags).GetValue(null));
        initialCount=(int)Call(group,"GetTotalCount");
        var sizeType=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");var enumType=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        foreach(var size in Sizes) Call(group,"AddCustomSize",Activator.CreateInstance(sizeType,Flags,null,new object[]{Enum.Parse(enumType,"FixedResolution"),size.x,size.y,"Celebration review"},null));
        shots.Clear();
        if (video)
        {
            Directory.CreateDirectory(Folder+"/"+mode+"-frames");
            int frames = Mathf.CeilToInt((timeline.Length(ResultTrack.Complete)+.1f)*30);
            for(int frame=0;frame<=frames;frame++) shots.Add(new Shot { Size=1,Time=frame/30f,Name=mode+"-frames/frame-"+frame.ToString("D3") });
        }
        else for(int size=0;size<3;size++)
        {
            var m=timeline.Celebration;
            float[] times={0,m.startTime+m.enterDuration*.45f,m.startTime+m.enterDuration,m.startTime+m.enterDuration+m.holdDuration*.5f,m.End-m.exitDuration*.5f,m.End-.0001f};
            string[] names={"before","entry","stop","hold","leaving","exit"};
            for(int phase=0;phase<times.Length;phase++) shots.Add(new Shot{Size=size,Time=times[phase],Name=names[phase]+"-"+Sizes[size].x+"x"+Sizes[size].y});
        }

        index=tick=0;running=true;EditorApplication.update+=Step;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;view.Show();view.Focus();
        return "Queued "+shots.Count+" native Game View frames ("+mode+").";
    }

    private static void Step()
    {
        try
        {
            Guard(); var shot=shots[index];var size=Sizes[shot.Size];
            if(tick==0)
            {
                view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,initialCount+shot.Size);
                game.DisplayState(GameState.Start);game.DisplayState(GameState.Complete);
                bus.Resize(size.x,size.y);
                var level=ScriptableObject.CreateInstance<LevelData>();level.busOrder.Add(PassengerColor.Purple);bus.Prepare(level);Object.DestroyImmediate(level);
            }
            Call(document.panelSettings,"ApplyPanelSettings");
            var panel=document.rootVisualElement.panel; if(panel==null)return;Call(panel,"ValidateLayout");
            var root=document.rootVisualElement.Q("ui-root");float aspect=(float)size.x/size.y;
            root.EnableInClassList("portrait",aspect<.8f);root.EnableInClassList("square",false);root.EnableInClassList("landscape",aspect>1.25f);
            Call(game,"SetCelebrationTime",shot.Time);Call(game,"SetCelebrationIdle",shot.Time);Call(game,"PauseResultTweens");
            EditorApplication.QueuePlayerLoopUpdate();
            view.Repaint();
            if(++tick<(index==0||shots[index-1].Size!=shot.Size?25:5))return;
            var target=Field<RenderTexture>(view,"m_RenderTexture");
            Check(target.width==size.x&&target.height==size.y,"Native target size mismatch.");
            VectorUiReview.Save(target,Folder+"/"+shot.Name+".png");
            File.AppendAllText(Folder+"/stats.tsv",shot.Name+"\t"+UnityStats.drawCalls+"\t"+UnityStats.triangles+"\t"+UnityStats.batches+"\n");
            File.AppendAllText(Folder+"/native-path.tsv",shot.Name+"\t"+shot.Time+"\t"+target.width+"x"+target.height+"\tGameView.OnGUI / native final target\tpanel.targetTexture=null\tZAA raster SVG\tbus MSAA="+bus.Target.antiAliasing+"\tPlay=false\n");
            tick=0;if(++index==shots.Count){Cleanup();File.WriteAllText(Folder+"/capture-done.txt",shots.Count+" native Game View captures completed.");}
        }
        catch(Exception error){File.WriteAllText(Folder+"/capture-error.txt",error.ToString());Cleanup();}
    }

    public static string Status() => running ? "Capturing "+index+" / "+shots.Count : "Idle";
    public static void Cleanup()
    {
        EditorApplication.update-=Step;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;running=false;
        if(game!=null)
        {
            game.DisplayState(GameState.Start);bus?.Dispose();
            typeof(ToolkitGameUI).GetField("celebrationBus",Flags).SetValue(game,null);
        }
        if(group!=null)
        {
            view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,originalIndex);
            for(int i=Sizes.Length-1;i>=0;i--)Call(group,"RemoveCustomSize",initialCount+i);
        }
        group=null;game=null;bus=null;document=null;

    }
}
#endif
