#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

/// <summary>Native Game View background comparisons; all temporary state is restored and never saved.</summary>
public static class ZScnBackgroundReview
{
    private const string Folder=ZScnBackground.Folder;
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    private static readonly Vector2Int[] Sizes={new Vector2Int(580,1250),new Vector2Int(1080,1920),new Vector2Int(2560,1080),new Vector2Int(1080,1080)};
    private static EditorWindow view;
    private static object group;
    private static int originalIndex,initialCount,index,tick;
    private static string mode;
    private static bool running,documentEnabled;
    private static UIDocument document;
    private static Vector3 cameraPosition;
    private static Matrix4x4 projection;
    private static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,Flags).Invoke(value,args);
    public static string Start(string label)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||running)throw new InvalidOperationException("Idle compiled Edit mode required.");
        mode=label;Directory.CreateDirectory(Folder);document=Object.FindObjectOfType<UIDocument>();documentEnabled=document.enabled;document.enabled=false;
        cameraPosition=Camera.main.transform.position;projection=Camera.main.projectionMatrix;
        view=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        originalIndex=(int)view.GetType().GetProperty("selectedSizeIndex",Flags).GetValue(view);
        var sizesType=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(Editor).Assembly.GetType("UnityEditor.ScriptableSingleton`1").MakeGenericType(sizesType);
        group=sizesType.GetProperty("currentGroup",Flags).GetValue(singleton.GetProperty("instance",Flags).GetValue(null));
        initialCount=(int)Call(group,"GetTotalCount");
        var sizeType=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");var enumType=typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        foreach(var size in Sizes)Call(group,"AddCustomSize",Activator.CreateInstance(sizeType,Flags,null,new object[]{Enum.Parse(enumType,"FixedResolution"),size.x,size.y,"Background review"},null));
        index=tick=0;running=true;EditorApplication.update+=Step;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;view.Show();view.Focus();
        return "Queued four native background Game View captures: "+label;
    }
    private static void Step()
    {
        try
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Never Play.");
            var size=Sizes[index];
            if(tick==0)view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,initialCount+index);
            var camera=Camera.main;camera.aspect=(float)size.x/size.y;
            camera.GetComponent<LayoutModeController>().ApplyViewport(size.x,size.y,new Rect(0,0,size.x,size.y));
            camera.GetComponent<CameraFitter>().Fit();EditorApplication.QueuePlayerLoopUpdate();view.Repaint();
            if(++tick<35)return;
            var target=(RenderTexture)view.GetType().GetField("m_RenderTexture",Flags).GetValue(view);
            if(target.width!=size.x||target.height!=size.y)throw new InvalidOperationException("Native size mismatch.");
            string name=mode+"-"+size.x+"x"+size.y;VectorUiReview.Save(target,Folder+"/"+name+".png");
            File.AppendAllText(Folder+"/stats.tsv",name+"\t"+UnityStats.drawCalls+"\t"+UnityStats.triangles+"\t"+UnityStats.batches+"\t"+UnityStats.setPassCalls+"\n");
            var text=new System.Text.StringBuilder();text.AppendLine("Native GameView m_RenderTexture "+size+"; UI hidden; Edit mode; raw readback untouched.");
            text.AppendLine("Camera "+camera.transform.position+"; FOV="+camera.fieldOfView+"; projection m12="+camera.projectionMatrix.m12);
            foreach(float z in new[]{4.703254f,8.650622f,10.768f,12.741531f,13.243923f})text.AppendLine("Band Z="+z+" viewportY="+camera.WorldToViewportPoint(new Vector3(0,-.3f,z)).y);
            foreach(var corner in new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.one})
            {
                var ray=camera.ViewportPointToRay(corner);float distance;new Plane(Vector3.up,new Vector3(0,-.3f,0)).Raycast(ray,out distance);text.AppendLine("Frustum ground corner "+ray.GetPoint(distance));
            }
            File.WriteAllText(Folder+"/"+name+".txt",text.ToString());
            tick=0;if(++index==Sizes.Length){Cleanup();File.WriteAllText(Folder+"/"+mode+"-done.txt","Four native Game View frames completed; no Play.");}
        }
        catch(Exception error){File.WriteAllText(Folder+"/capture-error.txt",error.ToString());Cleanup();}
    }
    public static string Status()=>running?"Capturing "+index+"/4":"Idle";
    public static void Cleanup()
    {
        EditorApplication.update-=Step;AssemblyReloadEvents.beforeAssemblyReload-=Cleanup;running=false;
        if(group!=null)
        {
            view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,originalIndex);
            for(int i=Sizes.Length-1;i>=0;i--)Call(group,"RemoveCustomSize",initialCount+i);
        }
        group=null;if(document!=null)document.enabled=documentEnabled;
        if(Camera.main!=null){Camera.main.transform.position=cameraPosition;Camera.main.projectionMatrix=projection;Camera.main.ResetAspect();Camera.main.GetComponent<LayoutModeController>().Refresh(true);}
    }
}
#endif
