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

/// <summary>Reads the normal Game View final target; never creates a UI capture RenderTexture.</summary>
public static class VectorUiReview
{
    public const string Folder="C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/aa";
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    private static readonly Vector2Int[] Sizes={new Vector2Int(580,1250),new Vector2Int(1080,1920),new Vector2Int(2560,1080)};
    private static ToolkitGameUI game;
    private static UIDocument document;
    private static EditorWindow view;
    private static object group;
    private static int originalIndex,initialCount,index,tick;
    private static string mode;

    public static void StartReview(bool after)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Application.unityVersion!="2022.3.19f1") throw new InvalidOperationException("Compiled Unity 2022.3.19f1 Edit mode required.");
        Cleanup(); mode=after?"after":"before";
        game=Object.FindObjectOfType<ToolkitGameUI>();document=game.GetComponent<UIDocument>();
        view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        originalIndex=(int)view.GetType().GetProperty("selectedSizeIndex",Flags).GetValue(view);
        var sizesType=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(Editor).Assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
        group=sizesType.GetProperty("currentGroup",Flags).GetValue(singleton.GetProperty("instance",Flags).GetValue(null));
        initialCount=(int)group.GetType().GetMethod("GetTotalCount",Flags).Invoke(group,null);
        var sizeType=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");var enumType=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        foreach(var size in Sizes) group.GetType().GetMethod("AddCustomSize",Flags).Invoke(group,new[]{Activator.CreateInstance(sizeType,Flags,null,new object[]{Enum.Parse(enumType,"FixedResolution"),size.x,size.y,"AA review"},null)});
        index=tick=0; Directory.CreateDirectory(Folder);
        EditorApplication.update+=Step;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;view.Show();view.Focus();
    }

    private static void Step()
    {
        try
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){Cleanup();return;}
            var size=Sizes[index];
            if(tick==0){view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,initialCount+index);game.DisplayState(GameState.Complete);}
            typeof(PanelSettings).GetMethod("ApplyPanelSettings",Flags).Invoke(document.panelSettings,null);
            document.rootVisualElement.panel.GetType().GetMethod("ValidateLayout",Flags).Invoke(document.rootVisualElement.panel,null);
            var root=document.rootVisualElement.Q("ui-root");
            root.EnableInClassList("portrait",(float)size.x/size.y<.8f);root.EnableInClassList("square",false);root.EnableInClassList("landscape",(float)size.x/size.y>1.25f);
            typeof(ToolkitGameUI).GetMethod("SetCelebrationTime",Flags).Invoke(game,new object[]{3.45f});
            typeof(ToolkitGameUI).GetMethod("PauseResultTweens",Flags).Invoke(game,null);
            view.SendEvent(new Event{type=EventType.Repaint});
            if(++tick<30){view.Repaint();return;}
            var result=(RenderTexture)view.GetType().GetField("m_RenderTexture",Flags).GetValue(view);
            Save(result,Folder+"/"+mode+"-"+size.x+"x"+size.y+".png");
            var bounds=new System.Text.StringBuilder();float scale=size.x/document.rootVisualElement.resolvedStyle.width;
            foreach(var name in new[]{"complete-title","result-coin-plate","reward-pile","continue-button"}){Rect b=root.Q(name).worldBound;bounds.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0}\t{1}\t{2}\t{3}\t{4}",name,b.x*scale,b.y*scale,b.width*scale,b.height*scale));}
            File.WriteAllText(Folder+"/bounds-"+size.x+"x"+size.y+".tsv",bounds.ToString());
            File.AppendAllText(Folder+"/raster-game-view-path.txt",mode+" "+result.width+"x"+result.height+" GameView.OnGUI native final target, MSAA="+result.antiAliasing+", panel.targetTexture=null, Play=false\n");
            tick=0;if(++index==Sizes.Length){Cleanup();File.WriteAllText(Folder+"/"+mode+"-done.txt","Three native Game View captures; Edit mode.");}
        }
        catch(Exception error){File.WriteAllText(Folder+"/raster-error.txt",error.ToString());Cleanup();}
    }

    public static void Save(RenderTexture target,string path)
    {
        var old=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=old;
        File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);
    }
    public static void Cleanup()
    {
        EditorApplication.update-=Step;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;
        if(game!=null)game.DisplayState(GameState.Start);
        if(group!=null){view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,originalIndex);for(int i=Sizes.Length-1;i>=0;i--)group.GetType().GetMethod("RemoveCustomSize",Flags).Invoke(group,new object[]{initialCount+i});}
        group=null;
    }
}
#endif
