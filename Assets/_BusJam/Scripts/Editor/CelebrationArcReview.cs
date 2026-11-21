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
public static class CelebrationArcReview
{
    public const string Folder = "C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/celeb5";
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
        string mode = "arc";
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
            float[] times={m.startTime,m.startTime+m.enterDuration*.2f,m.startTime+m.enterDuration,m.startTime+m.enterDuration+m.holdDuration*.5f,m.End-m.exitDuration*.5f,m.End-.0001f,m.startTime+m.enterDuration+.1f,m.startTime+m.enterDuration+.5f};
            string[] names={"enter","arc-mid","stop","hold","exit-arc-mid","exit","hold-early","hold-late"};
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

    private static Vector3[] BoneVertices(SkinnedMeshRenderer skin, Transform bone)
    {
        int index=Array.IndexOf(skin.bones,bone);var mesh=skin.sharedMesh;
        var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes[index];
        var points=new List<Vector3>();
        for(int i=0;i<vertices.Length;i++) if(weights[i].boneIndex0==index&&weights[i].weight0>.99f) points.Add(bind.MultiplyPoint3x4(vertices[i]));
        return points.ToArray();
    }

    // Measure within the rig hierarchy, avoiding float cancellation at the remote pooled stage.
    private static Matrix4x4 RigMatrix(Transform bone,Transform root)
    {
        var matrix=Matrix4x4.identity;
        for(var node=bone;node!=root;node=node.parent)matrix=Matrix4x4.TRS(node.localPosition,node.localRotation,node.localScale)*matrix;
        return matrix;
    }

    private static void CheckOffscreen(CompleteCelebration effect)
    {
        foreach(var renderer in Field<GameObject>(effect,"cast").GetComponentsInChildren<Renderer>(true))
        {
            if(!renderer.enabled||!renderer.gameObject.activeInHierarchy)continue;
            var bounds=renderer.bounds;float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,maxY=float.NegativeInfinity;
            for(int c=0;c<8;c++)
            {
                var p=effect.Camera.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)));
                minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);maxY=Mathf.Max(maxY,p.y);
            }
            Check(minX>1||maxX<0||maxY<0,"Endpoint retains visible renderer: "+renderer.name);
        }
    }

    public static string Verify()
    {
        Guard(); Check(!running,"Wait for native captures."); Directory.CreateDirectory(Folder);
        var review=typeof(ResultScreenReview);
        review.GetMethod("Create",Flags).Invoke(null,new object[]{1080,1920});
        var fixture=(ToolkitGameUI)review.GetField("game",Flags).GetValue(null);
        var source=AssetDatabase.LoadAssetAtPath<ResultTimeline>("Assets/_BusJam/Data/ResultTimeline.asset");
        string sourceJson=JsonUtility.ToJson(source);
        var asset=Object.Instantiate(source);asset.hideFlags=HideFlags.HideAndDontSave;
        typeof(ToolkitGameUI).GetField("resultTimeline",Flags).SetValue(fixture,asset);
        Call(fixture,"InitializeCelebrationBus");
        var effect=Field<CompleteCelebration>(fixture,"celebrationBus");effect.Camera.scene=fixture.gameObject.scene;
        var sample=(Action<float>)Delegate.CreateDelegate(typeof(Action<float>),fixture,typeof(ToolkitGameUI).GetMethod("SetCelebrationTime",Flags));
        var cast=Field<GameObject>(effect,"cast").transform;var vehicle=Field<Transform>(effect,"bus");
        var riders=Field<Transform[]>(effect,"riders");var upper=Field<Transform[]>(effect,"upperArms");var lower=Field<Transform[]>(effect,"lowerArms");
        var heads=Field<Transform[]>(effect,"heads");var feet=Field<Transform[]>(effect,"feet");var soles=Field<Vector3[]>(effect,"footSoles");
        var decks=Field<Vector3[]>(effect,"deckPositions");var heights=Field<float[]>(effect,"riderHeights");var skins=Field<Renderer[]>(effect,"riderBodies");
        var settings=Resources.Load<CelebrationSettings>("CelebrationSettings");
        var referenceRoot=new GameObject("Grid rig reference"){hideFlags=HideFlags.HideAndDontSave};referenceRoot.SetActive(false);
        var reference=Object.Instantiate(settings.passengerPrefab,referenceRoot.transform).transform;
        settings.idle.SampleAnimation(reference.GetComponentInChildren<Animator>(true).gameObject,0);
        var referenceRig=reference.GetComponentInChildren<Animator>(true).transform;
        var refBones=referenceRig.Find("YolcuRig").GetComponentsInChildren<Transform>(true);
        var riderBones=new Transform[3][];var handVertices=new Vector3[3][];var headVertices=new Vector3[3][];
        for(int i=0;i<3;i++)
        {
            riderBones[i]=Field<GameObject[]>(effect,"waveRoots")[i].transform.Find("YolcuRig").GetComponentsInChildren<Transform>(true);
            handVertices[i]=BoneVertices((SkinnedMeshRenderer)skins[i],lower[i]);headVertices[i]=BoneVertices((SkinnedMeshRenderer)skins[i],heads[i]);
        }
        int coins=BusJam.Save.SaveSystem.Coins;float peak=0,drift=0,headClearance=float.PositiveInfinity,boneError=0;int maxAir=0;
        var measurements=new System.Text.StringBuilder("resolution\trider\tarm\tgridUpperLength\tcelebrationUpperLength\tgridForearmMeshLength\tcelebrationForearmMeshLength\n");
        try
        {
            fixture.DisplayState(GameState.Complete);
            var m=asset.Celebration;float stopTime=m.startTime+m.enterDuration,exitTime=stopTime+m.holdDuration;
            foreach(var size in Sizes)
            {
                float tunedBounce=m.busBounceAmplitude;m.busBounceAmplitude=0;
                effect.Resize(size.x,size.y);sample(stopTime);Vector3 stopped=cast.position;
                Vector3 projected=effect.Camera.WorldToViewportPoint(stopped);
                Check(Vector2.Distance(new Vector2(projected.x,projected.y),m.stopPosition)<.0001f,"Stop position changed.");
                sample(m.startTime);projected=effect.Camera.WorldToViewportPoint(cast.position);
                Check(Vector2.Distance(new Vector2(projected.x,projected.y),m.enterStart)<.001f,"Arc start ignored: projected="+projected+" target="+m.enterStart);
                CheckOffscreen(effect);
                sample(m.startTime+m.enterDuration*.2f);Vector3 mid=effect.Camera.WorldToViewportPoint(cast.position);
                Check(mid.x<m.enterStart.x&&mid.x>m.stopPosition.x&&mid.y>m.enterStart.y,"Entry must arc up from bottom right.");
                float cross=(mid.x-m.enterStart.x)*(m.stopPosition.y-m.enterStart.y)-(mid.y-m.enterStart.y)*(m.stopPosition.x-m.enterStart.x);
                Check(Mathf.Abs(cross)>.01f,"Entry collapsed to a straight line.");
                sample(exitTime+.00001f);Check(Vector3.Distance(cast.position,stopped)<.001f,"Exit curve jumps at start.");
                sample(exitTime+m.exitDuration*.5f);mid=effect.Camera.WorldToViewportPoint(cast.position);
                Check(mid.x<m.stopPosition.x&&mid.y<m.stopPosition.y&&mid.x>m.exitEnd.x,"Exit must arc toward bottom left.");
                cross=(mid.x-m.stopPosition.x)*(m.exitEnd.y-m.stopPosition.y)-(mid.y-m.stopPosition.y)*(m.exitEnd.x-m.stopPosition.x);
                Check(Mathf.Abs(cross)>.01f,"Exit collapsed to a straight line.");
                sample(m.End-.00001f);projected=effect.Camera.WorldToViewportPoint(cast.position);
                Check(Vector2.Distance(new Vector2(projected.x,projected.y),m.exitEnd)<.002f,"Arc end ignored.");
                CheckOffscreen(effect);
                m.busBounceAmplitude=tunedBounce;
                for(int frame=0;frame<=650;frame++)
                {
                    float hold=m.holdDuration*frame/650;sample(stopTime+hold);
                    drift=Mathf.Max(drift,Vector3.Distance(stopped,cast.position));int air=0;
                    Check(Quaternion.Angle(cast.rotation,Quaternion.identity)<.001f,"Hold tilt did not settle.");
                    for(int i=0;i<3;i++)
                    {
                        float lift=Vector3.Dot(feet[i].TransformPoint(soles[i])-vehicle.TransformPoint(decks[i]),vehicle.up);
                        float ratio=lift/(heights[i]*cast.localScale.y);peak=Mathf.Max(peak,ratio);
                        Check(ratio>=-.002f&&ratio<=m.riderHopHeight+.001f,"Foot penetrated deck or exceeded tuned hop height.");if(ratio>.002f)air++;
                        var local=vehicle.InverseTransformPoint(feet[i].TransformPoint(soles[i]));
                        Check(Mathf.Abs(local.z)<.8f&&Mathf.Abs(local.x)<3.5f,"Rider left deck footprint.");
                        settings.idle.SampleAnimation(referenceRig.gameObject,Mathf.Max(0,hold-i*m.riderHopStagger)%settings.idle.length);
                        for(int b=0;b<refBones.Length;b++)
                        {
                            Check(refBones[b].name==riderBones[i][b].name,"Rig differs from grid.");
                            boneError=Mathf.Max(boneError,Vector3.Distance(refBones[b].localPosition,riderBones[i][b].localPosition),Vector3.Distance(refBones[b].localScale,riderBones[i][b].localScale));
                        }
                        float handTop=float.NegativeInfinity,headTop=float.NegativeInfinity;
                        foreach(var v in handVertices[i])handTop=Mathf.Max(handTop,riders[i].InverseTransformPoint(lower[i].TransformPoint(v)).y);
                        foreach(var v in headVertices[i])headTop=Mathf.Max(headTop,riders[i].InverseTransformPoint(heads[i].TransformPoint(v)).y);
                        headClearance=Mathf.Min(headClearance,headTop-handTop);
                        Check(handTop<headTop,"Wave hand rose above head.");
                        Vector3 u=riders[i].InverseTransformDirection(upper[i].up),l=riders[i].InverseTransformDirection(lower[i].up);
                        Check(u.y>0&&u.y<.6f&&l.y>.6f&&Vector3.Angle(u,l)>20&&Vector3.Angle(u,l)<130,"Unnatural shoulder/elbow joint pose: rider="+i+" hold="+hold+" upper="+u+" lower="+l+" angle="+Vector3.Angle(u,l));
                    }
                    maxAir=Mathf.Max(maxAir,air);
                }
                for(int i=0;i<3;i++)
                {
                    var ru=referenceRig.Find("YolcuRig/Root/Hips/Spine/UpperArm."+(i%2==0?"R":"L"));var rl=ru.GetChild(0);
                    float gridLength=(RigMatrix(rl,reference).MultiplyPoint3x4(Vector3.zero)-RigMatrix(ru,reference).MultiplyPoint3x4(Vector3.zero)).magnitude;
                    float length=(RigMatrix(lower[i],riders[i]).MultiplyPoint3x4(Vector3.zero)-RigMatrix(upper[i],riders[i]).MultiplyPoint3x4(Vector3.zero)).magnitude;
                    var referenceSkin=reference.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    var points=BoneVertices(referenceSkin,rl);float gridForearm=0,forearm=0;
                    foreach(var v in points)gridForearm=Mathf.Max(gridForearm,RigMatrix(rl,reference).MultiplyVector(v).magnitude);
                    foreach(var v in handVertices[i])forearm=Mathf.Max(forearm,RigMatrix(lower[i],riders[i]).MultiplyVector(v).magnitude);
                    Check(Mathf.Abs(gridLength-length)<.00001f&&Mathf.Abs(gridForearm-forearm)<.00001f,"Bone lengths differ from grid passenger.");
                    measurements.AppendLine(size.x+"x"+size.y+"\t"+i+"\t"+(i%2==0?"R":"L")+"\t"+gridLength+"\t"+length+"\t"+gridForearm+"\t"+forearm);
                }
                sample(m.End);Check(!effect.Visible,"End must hide pooled camera.");
            }
            Check(drift<.00001f&&boneError<.00001f&&maxAir<=2,"Hold drift="+drift+" bone error="+boneError+" airborne="+maxAir);
            foreach(var field in typeof(CelebrationChoreography).GetFields(BindingFlags.Instance|BindingFlags.Public))Check(Attribute.IsDefined(field,typeof(TooltipAttribute)),"Missing English tooltip: "+field.Name);
            m.pathTilt=0;sample(m.startTime+.1f);Check(Quaternion.Angle(cast.rotation,Quaternion.identity)<.001f,"Zero tilt ignored.");
            m.pathTilt=5;sample(m.startTime+.1f);Check(Quaternion.Angle(cast.rotation,Quaternion.identity)>0&&Quaternion.Angle(cast.rotation,Quaternion.identity)<=5.01f,"Arc tilt ignored.");
            m.waveSpeed=0;sample(stopTime+.2f);var frozen=lower[0].localRotation;sample(stopTime+.3f);Check(Quaternion.Angle(frozen,lower[0].localRotation)<.01f,"Zero wave speed ignored.");
            m.waveSpeed=1;m.waveAmplitude=0;sample(stopTime+.2f);frozen=lower[0].localRotation;sample(stopTime+.3f);Check(Quaternion.Angle(frozen,lower[0].localRotation)<.01f,"Zero wave amplitude ignored.");
            m.waveAmplitude=25;m.waveArmRaise=0;sample(stopTime+.3f);Check(Mathf.Abs(riders[0].InverseTransformDirection(upper[0].up).y)<.03f,"Wave raise ignored.");
            JsonUtility.FromJsonOverwrite(sourceJson,asset);asset.Rebuild();m=asset.Celebration;
            m.pathMode=CelebrationPathMode.Straight;
            foreach(CelebrationEnterSide side in Enum.GetValues(typeof(CelebrationEnterSide)))
            {m.enterSide=side;sample(m.startTime+.05f);float x=cast.position.x;sample(m.startTime+.15f);Check(side==CelebrationEnterSide.Left?cast.position.x>x:cast.position.x<x,"Legacy entry ignored.");}
            m.exitEase=AnimationCurve.Linear(0,0,1,1);
            foreach(CelebrationExitDirection direction in Enum.GetValues(typeof(CelebrationExitDirection)))
            {m.exitDirection=direction;sample(exitTime+.1f);Vector3 p=cast.position;sample(exitTime+.3f);Vector3 d=cast.position-p;Check(direction==CelebrationExitDirection.Down?Vector3.Dot(d,effect.Camera.transform.up)<0:direction==CelebrationExitDirection.Left?d.x<0:d.x>0,"Legacy exit ignored.");}
            JsonUtility.FromJsonOverwrite(sourceJson,asset);asset.Rebuild();m=asset.Celebration;
            for(int i=0;i<20;i++){fixture.DisplayState(GameState.Start);fixture.DisplayState(GameState.Complete);for(int f=0;f<107;f++)sample(f/30f);}
            int transforms=effect.Stage.GetComponentsInChildren<Transform>(true).Length,textures=Resources.FindObjectsOfTypeAll<RenderTexture>().Length,materials=Resources.FindObjectsOfTypeAll<Material>().Length;
            int elements=CountElements(fixture.GetComponent<UIDocument>().rootVisualElement),tweens=DG.Tweening.DOTween.TotalActiveTweens(),identity=effect.Target.GetInstanceID();
            for(int i=0;i<50;i++)
            {fixture.DisplayState(GameState.Start);fixture.DisplayState(GameState.Complete);for(int f=0;f<107;f++)sample(f/30f);Call(fixture,"OnDisable");Check(!effect.Visible,"Disable retained celebration.");Call(fixture,"OnEnable");}
            Check(transforms==effect.Stage.GetComponentsInChildren<Transform>(true).Length&&textures==Resources.FindObjectsOfTypeAll<RenderTexture>().Length&&materials==Resources.FindObjectsOfTypeAll<Material>().Length&&elements==CountElements(fixture.GetComponent<UIDocument>().rootVisualElement)&&tweens==DG.Tweening.DOTween.TotalActiveTweens()&&identity==effect.Target.GetInstanceID(),"Retained resource growth.");
            for(int i=0;i<400;i++)sample(m.startTime+i%180/100f);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<400;i++)sample(m.startTime+i%180/100f);
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Check(bytes==0,"Production sampler allocated "+bytes);
            Check(sourceJson==JsonUtility.ToJson(source)&&BusJam.Save.SaveSystem.Coins==coins,"Source timeline or saved coins changed.");
            File.WriteAllText(Folder+"/bone-lengths.tsv",measurements.ToString());
            string report="PASS: 1953 hold samples at three resolutions; drift="+drift+", max bone local-position/scale error="+boneError+", minimum hand clearance below head top="+headClearance+" rider units.\n"
                +"PASS: grid/celebration upper-arm and forearm lengths equal, R/L/R alternate arms, shoulder/elbow limits, hand never above head, deck contact, peak hop="+peak+", max airborne="+maxAir+".\n"
                +"PASS: curved right-bottom entry/left-bottom exit, authored easing (including descending exit), exact stop, endpoint continuity, zero/positive tilt, wave raise/speed/amplitude controls, Straight legacy sides/exits, all tooltips.\n"
                +"PASS: 20 warmup + 50 Complete/disable/enable cycles: 0 retained growth; transforms="+transforms+", UI elements="+elements+", textures="+textures+", materials="+materials+", tweens="+tweens+". Same render target identity.\n"
                +"400 warmed production frames: "+bytes+" managed bytes. Source timeline and saved coins unchanged. Edit mode only.\n";
            File.WriteAllText(Folder+"/validation.txt",report);return report;
        }
        finally {review.GetMethod("Cleanup",Flags).Invoke(null,null);Object.DestroyImmediate(asset);Object.DestroyImmediate(referenceRoot);}
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

    }
}
#endif
