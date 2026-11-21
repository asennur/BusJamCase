#if UNITY_EDITOR
using System;
using System.Reflection;
using BusJam.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Isolated, deterministic Edit-mode playback. Only the timeline asset is editable.</summary>
public sealed class ResultTimelinePreview : EditorWindow
{
    private ResultTimeline timeline;
    private ResultTrack track;
    private float time;
    private bool playing;
    private bool showPath = true;
    private double previousTick;
    private int sizeIndex;
    private static readonly Vector2Int[] Sizes = { new Vector2Int(1080,1920),new Vector2Int(580,1250),new Vector2Int(2560,1080) };
    private static readonly string[] SizeLabels = { "1080 x 1920", "580 x 1250", "2560 x 1080" };
    private Scene scene;
    private GameObject host, cameraHost;
    private ToolkitGameUI game;
    private UIDocument document;
    private Camera camera;
    private PanelSettings panel;
    private RenderTexture target;
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static readonly MethodInfo ApplyPanel=typeof(PanelSettings).GetMethod("ApplyPanelSettings",Flags);
    private Action applySettings, validateLayout;
    private Action<Event> repaintPanel;
    private Event repaintEvent;
    private int renderedRevision = -1, renderedSize = -1;
    private float renderedTime = -1;
    private ResultTrack renderedTrack;

    [MenuItem("Bus Jam/Result Timeline Preview")]
    public static void OpenMenu() => Open(AssetDatabase.LoadAssetAtPath<ResultTimeline>("Assets/_BusJam/Data/ResultTimeline.asset"));
    public static void Open(ResultTimeline asset)
    {
        var window=GetWindow<ResultTimelinePreview>("Result Timeline Preview");
        window.timeline=asset;window.minSize=new Vector2(420,500);window.Show();
    }
    private void OnEnable()
    {
        timeline=AssetDatabase.LoadAssetAtPath<ResultTimeline>("Assets/_BusJam/Data/ResultTimeline.asset");
        repaintEvent=new Event { type=EventType.Repaint };
        previousTick=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick; AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;
        EditorApplication.projectChanged+=Cleanup;
        EditorApplication.playModeStateChanged+=OnPlayState;
    }
    private void OnPlayState(PlayModeStateChange state) { playing=false;Cleanup(); }
    private void OnDisable()
    {
        EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;
        EditorApplication.projectChanged-=Cleanup;
        EditorApplication.playModeStateChanged-=OnPlayState;Cleanup();
    }

    private void OnGUI()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorGUILayout.HelpBox("Preview is available in Edit mode.",MessageType.Info);return;}
        var chosen=(ResultTimeline)EditorGUILayout.ObjectField("Timeline",timeline,typeof(ResultTimeline),false);
        if(chosen!=timeline){Cleanup();timeline=chosen;}
        int size=EditorGUILayout.Popup("Preview size",sizeIndex,SizeLabels);
        if(size!=sizeIndex){Cleanup();sizeIndex=size;}
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("Play Complete")) Play(ResultTrack.Complete);
            if(GUILayout.Button("Play Fail")) Play(ResultTrack.Fail);
            if(GUILayout.Button("Pause")) playing=false;
        }
        using(new EditorGUILayout.HorizontalScope())
        {
            if(GUILayout.Button("Coin Flight")) Play(ResultTrack.Collection);
            if(GUILayout.Button("Retry Fade")) Play(ResultTrack.Retry);
            if(GUILayout.Button("Edit Timings")) Selection.activeObject=timeline;
        }
        var chosenTrack=(ResultTrack)EditorGUILayout.EnumPopup("Track",track);
        if(chosenTrack!=track){track=chosenTrack;time=0;playing=false;}
        if(timeline==null)return;
        EditorGUI.BeginChangeCheck();
        float sampled=EditorGUILayout.Slider("Time (seconds)",time,0,Mathf.Max(.001f,timeline.Length(track)));
        if(EditorGUI.EndChangeCheck()){time=sampled;playing=false;}
        EditorGUILayout.LabelField("Edit-mode preview • No progress or scene saves",EditorStyles.miniLabel);
        showPath = EditorGUILayout.Toggle("Show celebration path", showPath);
        var rect=GUILayoutUtility.GetRect(100,100,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
        if(target!=null) EditorGUI.DrawPreviewTexture(rect,target,null,ScaleMode.ScaleToFit);
        else EditorGUI.LabelField(rect,"Preparing isolated preview. Open the Game scene if it is not loaded.");
        if(target!=null && showPath && track==ResultTrack.Complete && timeline.Celebration.pathMode==CelebrationPathMode.Arc)
        {
            float scale=Mathf.Min(rect.width/target.width,rect.height/target.height);
            var fit=new Rect(rect.center-new Vector2(target.width,target.height)*scale*.5f,new Vector2(target.width,target.height)*scale);
            GUI.BeginClip(fit); Handles.BeginGUI();
            var motion=timeline.Celebration;
            DrawArc(fit.size,motion.enterStart,motion.enterArcControl,motion.stopPosition,Color.cyan);
            DrawArc(fit.size,motion.stopPosition,motion.exitArcControl,motion.exitEnd,Color.yellow);
            Handles.EndGUI(); GUI.EndClip();
        }
    }

    private static void DrawArc(Vector2 size,Vector2 start,Vector2 control,Vector2 end,Color color)
    {
        Handles.color=color;
        Vector2 previous=new Vector2(start.x*size.x,(1-start.y)*size.y);
        for(int i=1;i<=32;i++)
        {
            Vector2 p=CelebrationChoreography.Arc(start,control,end,i/32f);
            Vector2 next=new Vector2(p.x*size.x,(1-p.y)*size.y);
            Handles.DrawLine(previous,next); previous=next;
        }
    }

    private void Play(ResultTrack value) { track=value;time=0;playing=true;previousTick=EditorApplication.timeSinceStartup; }
    private void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||timeline==null)return;
        double now=EditorApplication.timeSinceStartup;
        if(playing){time=Mathf.Min(timeline.Length(track),time+(float)(now-previousTick));if(time>=timeline.Length(track))playing=false;}
        previousTick=now;
        if(target!=null&&!playing&&renderedRevision==timeline.Revision&&renderedTime==time&&renderedTrack==track&&renderedSize==sizeIndex)return;
        if(game==null&&!Create())return;
        if(applySettings==null)applySettings=(Action)Delegate.CreateDelegate(typeof(Action),panel,ApplyPanel);
        applySettings();
        var runtimePanel=document.rootVisualElement.panel;
        if(runtimePanel==null)return;
        if(validateLayout==null)
        {
            validateLayout=(Action)Delegate.CreateDelegate(typeof(Action),runtimePanel,runtimePanel.GetType().GetMethod("ValidateLayout",Flags));
            repaintPanel=(Action<Event>)Delegate.CreateDelegate(typeof(Action<Event>),runtimePanel,runtimePanel.GetType().GetMethod("Repaint",Flags));
        }
        validateLayout();
        var size=Sizes[sizeIndex];
        if(!game.SampleResultPreview(track,time,size.x,size.y))return;
        applySettings();validateLayout();
        camera.Render();game.RenderPreviewCelebration();
        repaintPanel(repaintEvent);
        renderedRevision=timeline.Revision;renderedTime=time;renderedTrack=track;renderedSize=sizeIndex;
        Repaint();
    }

    private bool Create()
    {
        var original=Object.FindObjectOfType<ToolkitGameUI>();
        if(original==null)return false;
        var originalCamera=Camera.main;if(originalCamera==null)return false;
        var size=Sizes[sizeIndex];scene=EditorSceneManager.NewPreviewScene();
        host=new GameObject("Result Timeline Preview"){hideFlags=HideFlags.HideAndDontSave};
        SceneManager.MoveGameObjectToScene(host,scene);host.SetActive(false);
        target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32){name="Result Timeline Preview",hideFlags=HideFlags.HideAndDontSave};target.Create();
        panel=Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/_BusJam/UI/GamePanelSettings.asset"));
        panel.hideFlags=HideFlags.HideAndDontSave;panel.targetTexture=target;panel.clearColor=false;
        document=host.AddComponent<UIDocument>();document.panelSettings=panel;document.visualTreeAsset=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_BusJam/UI/GameUI.uxml");
        game=host.AddComponent<ToolkitGameUI>();EditorUtility.CopySerialized(original,game);game.ConfigureResultPreview(document,timeline);host.SetActive(true);
        cameraHost=new GameObject("Result Preview World Camera"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(cameraHost,scene);
        camera=cameraHost.AddComponent<Camera>();camera.CopyFrom(originalCamera);camera.transform.SetPositionAndRotation(originalCamera.transform.position,originalCamera.transform.rotation);
        camera.scene=originalCamera.scene;camera.targetTexture=target;camera.aspect=(float)size.x/size.y;camera.enabled=false;
        return true;
    }

    private void Cleanup()
    {
        if(host!=null)DestroyImmediate(host);if(cameraHost!=null)DestroyImmediate(cameraHost);
        if(panel!=null)DestroyImmediate(panel);
        if(target!=null){target.Release();DestroyImmediate(target);}
        if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
        host=cameraHost=null;game=null;document=null;camera=null;panel=null;target=null;repaintPanel=null;applySettings=validateLayout=null;
    }
}
#endif
