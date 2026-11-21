#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using BusJam.Core;
using BusJam.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Captures the native Start Game View in Edit mode without changing saved game state.</summary>
public static class ZScnMenuReview
{
    public const string Folder = "C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-3";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Vector2Int[] Sizes = { new Vector2Int(580,1250), new Vector2Int(1080,1920), new Vector2Int(2560,1080), new Vector2Int(1080,1080) };
    private static EditorWindow view;
    private static object group;
    private static int originalIndex, initialCount, index, tick;
    private static string label;
    private static bool running;
    private static bool includeUi;
    private static StyleEnum<Visibility> oldVisibility;
    private static ToolkitGameUI ui;
    private static UIDocument document;
    private static object Call(object value, string name, params object[] args) => value.GetType().GetMethod(name, Flags).Invoke(value,args);
    public static string Start(string name, bool showUi = true)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || running || Application.unityVersion != "2022.3.19f1") throw new InvalidOperationException("Idle compiled Edit mode required.");
        Directory.CreateDirectory(Folder); label = name;
        ui = Object.FindObjectOfType<ToolkitGameUI>(); document = ui.GetComponent<UIDocument>();
        includeUi = showUi; oldVisibility = document.rootVisualElement.style.visibility;
        document.rootVisualElement.style.visibility = showUi ? Visibility.Visible : Visibility.Hidden;
        view = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        originalIndex = (int)view.GetType().GetProperty("selectedSizeIndex",Flags).GetValue(view);
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(Editor).Assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
        group = sizesType.GetProperty("currentGroup",Flags).GetValue(singleton.GetProperty("instance",Flags).GetValue(null));
        initialCount = (int)Call(group,"GetTotalCount");
        var sizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize"); var enumType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        foreach (var size in Sizes) Call(group,"AddCustomSize",Activator.CreateInstance(sizeType,Flags,null,new object[]{Enum.Parse(enumType,"FixedResolution"),size.x,size.y,"Menu review"},null));
        index = tick = 0; running = true; EditorApplication.update += Step; AssemblyReloadEvents.beforeAssemblyReload += Cleanup; view.Show(); view.Focus();
        return "Queued four native Start captures: " + label;
    }
    private static void Step()
    {
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Never Play.");
            var size = Sizes[index];
            if (tick == 0) { view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,initialCount+index); ui.DisplayState(GameState.Start); }
            Call(document.panelSettings,"ApplyPanelSettings");
            if (document.rootVisualElement.panel == null) return;
            Call(document.rootVisualElement.panel,"ValidateLayout");
            var root = document.rootVisualElement.Q("ui-root"); float aspect = (float)size.x/size.y;
            root.EnableInClassList("portrait",aspect < .8f); root.EnableInClassList("square",false); root.EnableInClassList("landscape",aspect > 1.25f);
            var menu = GameObject.Find("Menu Presentation");
            if (menu != null) menu.SendMessage("FitAspect",aspect,SendMessageOptions.DontRequireReceiver);
            EditorApplication.QueuePlayerLoopUpdate(); view.Repaint();
            if (++tick < 40) return;
            var target = (RenderTexture)view.GetType().GetField("m_RenderTexture",Flags).GetValue(view);
            if (target.width != size.x || target.height != size.y) throw new InvalidOperationException("Native target size mismatch.");
            string name = label+"-"+size.x+"x"+size.y;
            VectorUiReview.Save(target,Folder+"/"+name+".png");
            File.AppendAllText(Folder+"/stats.tsv",name+"\t"+UnityStats.drawCalls+"\t"+UnityStats.triangles+"\t"+UnityStats.batches+"\t"+UnityStats.setPassCalls+"\n");
            var presentation = Object.FindObjectOfType<MenuWorldPresentation>();
            string cameraInfo = presentation == null ? "Legacy UI background" : "FOV="+presentation.menuCamera.fieldOfView+"; distance="+Vector3.Distance(presentation.menuCamera.transform.position,presentation.cameraTarget.position)
                +"; crest viewport="+presentation.menuCamera.WorldToViewportPoint(presentation.menuWorld.transform.TransformPoint(new Vector3(0,4.2f,-21)));
            File.WriteAllText(Folder+"/"+name+".txt","Native GameView m_RenderTexture "+size+"; UI="+includeUi+"; raw readback untouched; Play=false.\n"+cameraInfo);
            tick = 0; if (++index == Sizes.Length) { Cleanup(); File.WriteAllText(Folder+"/"+label+"-done.txt","Four native Game View captures; Edit mode."); }
        }
        catch (Exception error) { File.WriteAllText(Folder+"/capture-error.txt",error.ToString()); Cleanup(); }
    }
    public static string Status() => running ? "Capturing "+index+"/4" : "Idle";
    public static void Cleanup()
    {
        EditorApplication.update -= Step; AssemblyReloadEvents.beforeAssemblyReload -= Cleanup; running = false;
        if (group != null)
        {
            view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,originalIndex);
            for (int i=Sizes.Length-1;i>=0;i--) Call(group,"RemoveCustomSize",initialCount+i);
        }
        group = null;
        if (document != null) document.rootVisualElement.style.visibility = oldVisibility;
        var menu = GameObject.Find("Menu Presentation"); if (menu != null) menu.SendMessage("RefreshView",SendMessageOptions.DontRequireReceiver);
    }
}
#endif
