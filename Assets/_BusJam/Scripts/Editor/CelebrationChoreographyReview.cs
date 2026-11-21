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
public static class CelebrationChoreographyReview
{
    public const string Folder = "C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/celeb4";
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
    private static string baseline;
    private static bool running, edited;
    private static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Flags).Invoke(value, args);
    private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, Flags).GetValue(value);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Guard()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && Application.unityVersion == "2022.3.19f1"
            && Application.dataPath == "C:/Users/Kullanici/Documents/BusJamCase/Assets", "Pinned compiled Edit mode required.");
    }

    public static string Start(bool video = false, bool alternate = false)
    {
        Guard(); Check(!running, "Capture already running."); Directory.CreateDirectory(Folder);
        timeline = AssetDatabase.LoadAssetAtPath<ResultTimeline>("Assets/_BusJam/Data/ResultTimeline.asset");
        baseline = JsonUtility.ToJson(timeline); edited = alternate;
        if (alternate)
        {
            timeline.Celebration.holdDuration = 2.1f;
            timeline.Celebration.enterSide = CelebrationEnterSide.Left;
            timeline.Celebration.exitDirection = CelebrationExitDirection.Right;
            timeline.Rebuild(); EditorUtility.SetDirty(timeline); AssetDatabase.SaveAssetIfDirty(timeline);
        }
        string mode = alternate ? "tuned" : "default";
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
            float[] times={m.startTime+m.enterDuration*.35f,m.startTime+m.enterDuration,m.startTime+m.enterDuration+m.holdDuration*.5f,m.End-m.exitDuration*.3f};
            string[] names={"enter","stop","hold","exit"};
            for(int phase=0;phase<4;phase++) shots.Add(new Shot{Size=size,Time=times[phase],Name=names[phase]+"-"+Sizes[size].x+"x"+Sizes[size].y});
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
            }
            Call(document.panelSettings,"ApplyPanelSettings");
            var panel=document.rootVisualElement.panel; if(panel==null)return;Call(panel,"ValidateLayout");
            var root=document.rootVisualElement.Q("ui-root");float aspect=(float)size.x/size.y;
            root.EnableInClassList("portrait",aspect<.8f);root.EnableInClassList("square",false);root.EnableInClassList("landscape",aspect>1.25f);
            Call(game,"SetCelebrationTime",shot.Time);Call(game,"SetCelebrationIdle",shot.Time);Call(game,"PauseResultTweens");
            if(bus.Visible)bus.Camera.Render();
            view.Repaint();
            if(++tick<(index==0||shots[index-1].Size!=shot.Size?25:5))return;
            var target=Field<RenderTexture>(view,"m_RenderTexture");
            Check(target.width==size.x&&target.height==size.y,"Native target size mismatch.");
            VectorUiReview.Save(target,Folder+"/"+shot.Name+".png");
            File.AppendAllText(Folder+"/native-path.tsv",shot.Name+"\t"+shot.Time+"\t"+target.width+"x"+target.height+"\tGameView.OnGUI / native final target\tpanel.targetTexture=null\tZAA raster SVG\tbus MSAA="+bus.Target.antiAliasing+"\tPlay=false\n");
            tick=0;if(++index==shots.Count){Cleanup();File.WriteAllText(Folder+"/capture-done.txt",shots.Count+" native Game View captures completed.");}
        }
        catch(Exception error){File.WriteAllText(Folder+"/capture-error.txt",error.ToString());Cleanup();}
    }

    public static string Verify()
    {
        Guard(); Check(!running,"Wait for native captures."); Directory.CreateDirectory(Folder);
        var review=typeof(ResultScreenReview);
        review.GetMethod("Create",Flags).Invoke(null,new object[]{1080,1920});
        var fixture=(ToolkitGameUI)review.GetField("game",Flags).GetValue(null);
        var asset=Object.Instantiate(AssetDatabase.LoadAssetAtPath<ResultTimeline>("Assets/_BusJam/Data/ResultTimeline.asset"));
        asset.hideFlags=HideFlags.HideAndDontSave;
        typeof(ToolkitGameUI).GetField("resultTimeline",Flags).SetValue(fixture,asset);
        Call(fixture,"InitializeCelebrationBus");
        var effect=Field<CompleteCelebration>(fixture,"celebrationBus");effect.Camera.scene=fixture.gameObject.scene;
        var level=ScriptableObject.CreateInstance<LevelData>();level.busOrder.Add(PassengerColor.Purple);
        var sample=(Action<float>)Delegate.CreateDelegate(typeof(Action<float>),fixture,typeof(ToolkitGameUI).GetMethod("SetCelebrationTime",Flags));
        var cast=Field<GameObject>(effect,"cast").transform;var vehicle=Field<Transform>(effect,"bus");
        var feet=Field<Transform[]>(effect,"feet");var soles=Field<Vector3[]>(effect,"footSoles");
        var decks=Field<Vector3[]>(effect,"deckPositions");var heights=Field<float[]>(effect,"riderHeights");
        int coins=BusJam.Save.SaveSystem.Coins;float peak=0,holdDrift=0;int maxAir=0;
        string defaults=JsonUtility.ToJson(asset);
        try
        {
            fixture.DisplayState(GameState.Complete);effect.Prepare(level);
            foreach(var size in Sizes)
            {
                effect.Resize(size.x,size.y);sample(1.6f);Vector3 stopped=cast.position;
                var skins=Field<Renderer[]>(effect,"riderBodies");var headBones=Field<Transform[]>(effect,"heads");
                for(int rider=0;rider<3;rider++)
                {
                    var skin=(SkinnedMeshRenderer)skins[rider];var mesh=skin.sharedMesh;
                    var vertices=mesh.vertices;var weights=mesh.boneWeights;var binds=mesh.bindposes;var bones=skin.bones;
                    int head=Array.IndexOf(bones,headBones[rider]);float top=float.NegativeInfinity;
                    for(int v=0;v<vertices.Length;v++)if(weights[v].boneIndex0==head&&weights[v].weight0>.99f)
                        top=Mathf.Max(top,bones[head].TransformPoint(binds[head].MultiplyPoint3x4(vertices[v])).y);
                    float actual=top-feet[rider].TransformPoint(soles[rider]).y;
                    Check(heights[rider]*cast.localScale.y/actual<1.01f,"Cached height overstates independent head-to-sole measurement.");
                }
                for(int frame=0;frame<=1200;frame++)
                {
                    float t=1.6f+frame*.001f;sample(t);holdDrift=Mathf.Max(holdDrift,Vector3.Distance(stopped,cast.position));
                    int air=0;
                    for(int i=0;i<3;i++)
                    {
                        Vector3 sole=feet[i].TransformPoint(soles[i]);
                        float lift=Vector3.Dot(sole-vehicle.TransformPoint(decks[i]),vehicle.up);
                        float ratio=lift/(heights[i]*cast.localScale.y);peak=Mathf.Max(peak,ratio);
                        Check(ratio>=-.002f&&ratio<=.401f,"Foot penetrated deck or exceeded 0.4 standing height.");
                        if(ratio>.002f)air++;
                        Vector3 local=vehicle.InverseTransformPoint(sole);
                        Check(Mathf.Abs(local.z)<.8f&&Mathf.Abs(local.x)<3.5f,"Rider left the open deck footprint.");
                    }
                    maxAir=Mathf.Max(maxAir,air);
                }
                Check(holdDrift<.00001f,"Bus drifted during hold.");
                sample(1.3f);Vector3 entrance=cast.position;sample(1.45f);
                Check(cast.position.x<entrance.x,"Default entry must approach from the right.");
                sample(3f);Vector3 exit=cast.position;sample(3.3f);
                Check(effect.Camera.WorldToViewportPoint(cast.position).y<effect.Camera.WorldToViewportPoint(exit).y,"Default exit must slide downward.");
                sample(asset.Celebration.End);Check(!effect.Visible,"End must hide the pooled camera.");
            }
            Check(maxAir<=2,"Default stagger allowed all riders airborne.");
            effect.Resize(1080,1920);sample(1.6f);
            Vector3 origin=effect.Camera.WorldToViewportPoint(cast.position);
            Check(Mathf.Abs(origin.x-.5f)<.001f&&Mathf.Abs(origin.y-.2f)<.001f,"Normalized stop position mismatch.");
            Check(Quaternion.Angle(vehicle.localRotation,Quaternion.identity)<.001f,"Bus must show its side at zero camera yaw.");
            foreach(var field in typeof(CelebrationChoreography).GetFields(BindingFlags.Instance|BindingFlags.Public))
                Check(Attribute.IsDefined(field,typeof(TooltipAttribute)),"Missing English tooltip: "+field.Name);
            var motion=asset.Celebration;
            motion.startTime=2;motion.enterDuration=.8f;motion.holdDuration=2;motion.exitDuration=.9f;asset.Rebuild();
            Check(Mathf.Abs(asset.Length(ResultTrack.Complete)-5.7f)<.001f,"Edited phases must extend Complete length.");
            sample(1.9f);Check(!effect.Visible,"Edited start ignored.");sample(3.5f);Check(effect.Visible,"Edited hold ignored.");
            motion.stopPosition=new Vector2(.35f,.28f);asset.Rebuild();sample(3.5f);origin=effect.Camera.WorldToViewportPoint(cast.position);
            Check(Vector2.Distance(new Vector2(origin.x,origin.y),motion.stopPosition)<.001f,"Edited stop ignored.");
            foreach(CelebrationExitDirection direction in Enum.GetValues(typeof(CelebrationExitDirection)))
            {
                motion.exitDirection=direction;asset.Rebuild();sample(4.8f);Vector3 from=cast.position;sample(5.4f);
                Vector3 delta=cast.position-from;
                Check(direction==CelebrationExitDirection.Down?Vector3.Dot(delta,effect.Camera.transform.up)<0:direction==CelebrationExitDirection.Left?delta.x<0:delta.x>0,"Exit direction ignored.");
            }
            foreach(CelebrationEnterSide side in Enum.GetValues(typeof(CelebrationEnterSide)))
            {
                motion.enterSide=side;asset.Rebuild();sample(2.1f);float x=cast.position.x;sample(2.3f);
                Check(side==CelebrationEnterSide.Left?cast.position.x>x:cast.position.x<x,"Entry side ignored.");
            }
            motion.cameraYaw=25;motion.cameraPitch=18;motion.cameraDistance=22;motion.cameraFov=42;motion.busScale=.7f;asset.Rebuild();sample(3.5f);
            Check(Mathf.Abs(effect.Camera.fieldOfView-42)<.001f&&Mathf.Abs(effect.Camera.transform.localPosition.magnitude-22)<.002f,"Camera controls ignored.");
            Check(Quaternion.Angle(effect.Camera.transform.rotation,Quaternion.Euler(18,25,0))<.01f,"Camera angles ignored.");
            float small=cast.localScale.x;motion.busScale=1.4f;asset.Rebuild();sample(3.5f);Check(Mathf.Abs(cast.localScale.x/small-2)<.001f,"Bus scale ignored.");
            JsonUtility.FromJsonOverwrite(defaults,asset);asset.Rebuild();
            motion=asset.Celebration;motion.riderHopHeight=0;asset.Rebuild();sample(1.9f);
            for(int i=0;i<3;i++)Check(Mathf.Abs(Vector3.Dot(feet[i].TransformPoint(soles[i])-vehicle.TransformPoint(decks[i]),vehicle.up))<.002f,"Zero hop still lifts rider.");
            motion.riderHopHeight=.4f;asset.Rebuild();
            for(int frame=0;frame<1200;frame++)
            {
                sample(1.6f+frame*.001f);
                for(int i=0;i<3;i++)Check(Vector3.Dot(feet[i].TransformPoint(soles[i])-vehicle.TransformPoint(decks[i]),vehicle.up)/(heights[i]*cast.localScale.y)<=.401f,"Maximum hop exceeded 0.4.");
            }
            motion.riderHopFrequency=0;asset.Rebuild();sample(1.9f);
            for(int i=0;i<3;i++)Check(Mathf.Abs(Vector3.Dot(feet[i].TransformPoint(soles[i])-vehicle.TransformPoint(decks[i]),vehicle.up))<.002f,"Zero frequency still hops.");
            motion.enterDuration=motion.holdDuration=motion.exitDuration=0;asset.Rebuild();sample(motion.startTime);Check(!effect.Visible,"Zero phases must hide safely.");
            JsonUtility.FromJsonOverwrite(defaults,asset);asset.Rebuild();
            for(int i=0;i<20;i++){fixture.DisplayState(GameState.Start);fixture.DisplayState(GameState.Complete);for(int f=0;f<107;f++)sample(f/30f);}
            int transforms=effect.Stage.GetComponentsInChildren<Transform>(true).Length;
            int textures=Resources.FindObjectsOfTypeAll<RenderTexture>().Length,materials=Resources.FindObjectsOfTypeAll<Material>().Length;
            int elements=CountElements(fixture.GetComponent<UIDocument>().rootVisualElement),tweens=DG.Tweening.DOTween.TotalActiveTweens();
            int identity=effect.Target.GetInstanceID();
            for(int i=0;i<50;i++)
            {
                fixture.DisplayState(GameState.Start);fixture.DisplayState(GameState.Complete);for(int f=0;f<107;f++)sample(f/30f);
                Call(fixture,"OnDisable");Check(!effect.Visible,"Disable retained celebration.");Call(fixture,"OnEnable");
            }
            Check(transforms==effect.Stage.GetComponentsInChildren<Transform>(true).Length&&textures==Resources.FindObjectsOfTypeAll<RenderTexture>().Length
                &&materials==Resources.FindObjectsOfTypeAll<Material>().Length&&elements==CountElements(fixture.GetComponent<UIDocument>().rootVisualElement)
                &&tweens==DG.Tweening.DOTween.TotalActiveTweens()&&identity==effect.Target.GetInstanceID(),"Retained resource growth.");
            for(int i=0;i<400;i++)sample(1.1f+i%70/30f);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<400;i++)sample(1.1f+i%70/30f);
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Check(bytes==0,"Production sampler allocated "+bytes);
            fixture.DisplayState(GameState.Complete);sample(2.2f);using(var down=PointerDownEvent.GetPooled())Call(fixture,"SkipCelebration",down);Check(!effect.Visible,"Tap skip retained bus.");
            using(var up=PointerUpEvent.GetPooled())Call(fixture,"ReleaseSkipPointer",up);
            foreach(PassengerColor color in Enum.GetValues(typeof(PassengerColor)))
            {
                level.busOrder[0]=color;effect.Prepare(level);sample(2.2f);
                var expected=Resources.Load<CelebrationSettings>("CelebrationSettings").colors[(int)color];
                Check(Field<Renderer>(effect,"busBody").sharedMaterials[0]==expected,"Last bus colour mismatch.");
                foreach(var body in Field<Renderer[]>(effect,"riderBodies"))Check(body.sharedMaterial==expected,"Rider colour mismatch.");
            }
            fixture.DisplayState(GameState.Fail);Check(!effect.Visible,"Fail retained bus.");Check(BusJam.Save.SaveSystem.Coins==coins,"Saved coins changed.");
            string report="PASS: three resolutions, 3603 dense hold samples, stationary bus drift="+holdDrift+", peak default hop="+peak+" standing heights; maximum "+maxAir+" riders airborne.\n"
                +"PASS: deck footprint, max-height hop, zero hop/frequency/durations, normalized stop, side profile, all tooltips, edited timing/length, both entry sides, three exits, camera and scale controls.\n"
                +"PASS: 20 warmup cycles + 50 Complete/disable/enable cycles; 0 retained growth. Transforms="+transforms+", UI elements="+elements+", textures="+textures+", materials="+materials+", tweens="+tweens+"; same target identity.\n"
                +"400 warmed production sampler frames: "+bytes+" managed bytes. Last colour, three riders, pointer skip, Fail/disable cleanup and saved coins pass. Edit mode only.\n";
            File.WriteAllText(Folder+"/validation.txt",report);return report;
        }
        finally { review.GetMethod("Cleanup",Flags).Invoke(null,null);Object.DestroyImmediate(asset);Object.DestroyImmediate(level); }
    }
    private static int CountElements(VisualElement element)
    { int count=1;foreach(var child in element.Children())count+=CountElements(child);return count; }

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
        if(edited&&timeline!=null)
        {
            JsonUtility.FromJsonOverwrite(baseline,timeline);timeline.Rebuild();EditorUtility.SetDirty(timeline);AssetDatabase.SaveAssetIfDirty(timeline);
            File.WriteAllText(Folder+"/restored-asset.json",EditorJsonUtility.ToJson(timeline,true));
        }
        edited=false;
    }
}
#endif
