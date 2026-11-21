#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BusJam.Core;
using BusJam.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Checks saved menu bindings, extracted transforms, state events and allocation-free camera fitting.</summary>
public static class ZScnMenuValidation
{
    [Serializable] private class Layout { public Item[] objects; }
    [Serializable] private class Item { public string name; public float[] position, quaternion_xyzw, scale; }
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Application.unityVersion!="2022.3.19f1") throw new InvalidOperationException("Compiled Edit mode required.");
        var text = new StringBuilder(); int checks = 0;
        Action<bool,string> check = (valid,message)=> { if (!valid) throw new InvalidOperationException(message); checks++; text.AppendLine("PASS "+message); };
        var p = Object.FindObjectOfType<MenuWorldPresentation>(); var world = p.menuWorld.transform;
        check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Count(g=>g.name=="MenuWorld")==1,"One menu world root, including inactive roots");
        var source = JsonUtility.FromJson<Layout>(File.ReadAllText(ZScnMenu.Folder+"/MenuKit-layout.json"));
        foreach (var item in source.objects)
        {
            var t = world.Find(item.name); var q = item.quaternion_xyzw;
            check(t != null && Vector3.Distance(t.localPosition,new Vector3(item.position[0],item.position[1],item.position[2]))<.00001f
                && Vector3.Distance(t.localScale,new Vector3(item.scale[0],item.scale[1],item.scale[2]))<.00001f
                && Quaternion.Angle(t.localRotation,new Quaternion(q[0],q[1],q[2],q[3]))<.05f,"Extracted transform "+item.name);
        }
        check(world.position==new Vector3(1000,0,1000),"Menu world isolated at (1000,0,1000)");
        check(Mathf.Abs(GameObject.Find("Directional Light").GetComponent<Light>().intensity-.75f)<.000001f,"Original directional light intensity 0.75");
        check(world.GetComponentsInChildren<Collider>(true).Length==0,"Menu has no colliders");
        check(world.Find("Preview Ground").GetComponent<MeshFilter>().sharedMesh.normals.All(n=>n.y>.99f),"Preview ground faces the camera from above");
        check(world.Find("Preview Ground").GetComponent<MeshRenderer>().sharedMaterial.renderQueue==3000,"Ground fades after opaque background rendering");
        check(p.menuCamera.GetUniversalAdditionalCameraData().cameraStack.Count==0,"Menu camera has no overlay stack");
        var root = Object.FindObjectOfType<UIDocument>().rootVisualElement;
        check(root.Q("menu-background")==null,"Legacy menu UI background removed");
        foreach (var name in new[]{"start-screen","start-button","start-settings","lives-count","coin-count"}) check(root.Q(name)!=null,"Original UI retained: "+name);
        var publisher = typeof(GameManager).GetField("StateChanged",BindingFlags.Instance|BindingFlags.NonPublic);
        var stateBefore = p.gameManager.State;
        var originalPose = p.gameplayCamera.transform.position; var originalRotation = p.gameplayCamera.transform.rotation;
        var originalProjection = p.gameplayCamera.projectionMatrix;
        try
        {
            foreach (var state in new[]{GameState.Start,GameState.Ready,GameState.Playing,GameState.Complete,GameState.Fail,GameState.Start,GameState.Ready,GameState.Start})
            {
                ((Action<GameState>)publisher.GetValue(p.gameManager)).Invoke(state);
                bool start = state==GameState.Start;
                check(p.menuWorld.activeSelf==start && p.menuCamera.enabled==start,"State event toggles menu world/camera: "+state);
                check(p.gameplayCamera.enabled && p.gameplayCameraData.renderType==(start?CameraRenderType.Overlay:CameraRenderType.Base),"State event switches gameplay rendering: "+state);
                check(Camera.main==p.gameplayCamera,"Camera.main remains the gameplay camera: "+state);
            }
            check(p.gameManager.State==stateBefore,"Presentation tests do not change GameManager state");
            check(p.gameplayCamera.transform.position==originalPose && p.gameplayCamera.transform.rotation==originalRotation && p.gameplayCamera.projectionMatrix==originalProjection,"Gameplay camera pose and projection unchanged by state switches");
            foreach (var size in new[]{new Vector2Int(580,1250),new Vector2Int(1080,1920),new Vector2Int(2560,1080),new Vector2Int(1080,1080)})
            {
                float aspect=(float)size.x/size.y; p.menuCamera.aspect=aspect; p.FitAspect(aspect);
                var lens=LayoutModeController.Classify(aspect)==LayoutMode.Portrait?p.portraitLens:LayoutModeController.Classify(aspect)==LayoutMode.Square?p.squareLens:p.landscapeLens;
                check(Mathf.Abs(p.menuCamera.fieldOfView-lens.x)<.001f && Mathf.Abs(Vector3.Distance(p.menuCamera.transform.position,p.cameraTarget.position)-lens.y)<.0002f,"Aspect lens "+size+": "+lens);
                float expectedHeight=2*p.sky.localPosition.z*Mathf.Tan(lens.x*Mathf.Deg2Rad*.5f);
                check(p.sky.localScale.y>=expectedHeight-.001f && p.sky.localScale.x>=expectedHeight*aspect-.001f,"Sky covers view: "+size);
                check(Mathf.Abs(p.sky.localScale.x/p.sky.localScale.y-1080f/1920)<.00001f,"Sky retains image aspect: "+size);
                if (aspect<.8f)
                {
                    float crest=1-p.menuCamera.WorldToViewportPoint(world.TransformPoint(new Vector3(0,4.2f,-21))).y;
                    check(Mathf.Abs(crest-.39f)<.015f,"Portrait crest near 39% from top: "+crest);
                }
            }
            check(p.sky.parent==p.menuCamera.transform && p.sky.localPosition.z==200 && p.menuCamera.farClipPlane>200,"Sky is a far camera-child quad");
            check(p.sky.GetComponent<Renderer>().sharedMaterial.shader.name=="Universal Render Pipeline/Unlit","Sky uses Unlit shader");
            foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.transform!=p.sky))
            {
                check((GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)&StaticEditorFlags.BatchingStatic)!=0,"Static scenery: "+renderer.name);
                check(renderer.sharedMaterials.All(m=>m.enableInstancing),"Instanced shared materials: "+renderer.name);
            }
            foreach (var name in new[]{"MenuPalette","MenuAsphalt","MenuWalk"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_BusJam/Art/Models/Menu/"+name+".png"); bool palette=name=="MenuPalette";
                check(importer.sRGBTexture && importer.mipmapEnabled!=palette && importer.filterMode==(palette?FilterMode.Point:FilterMode.Trilinear)
                    && importer.wrapMode==(palette?TextureWrapMode.Clamp:TextureWrapMode.Repeat),"Texture import "+name);
            }
            var controller=(AnimatorController)p.busAnimator.runtimeAnimatorController;
            check(controller.layers[0].stateMachine.defaultState.name=="Idle","Bus controller defaults to Idle");
            var idle=(AnimationClip)controller.layers[0].stateMachine.defaultState.motion;
            check(idle.isLooping,"Bus Idle loops");
            p.busAnimator.Rebind(); p.busAnimator.Play("Idle",0,0); p.busAnimator.Update(0);
            var smoke=p.busAnimator.transform.Find("OtobusRig/Root/Smoke_0"); var first=smoke.localScale;
            p.busAnimator.Update(.23f);
            check(smoke.localScale!=first && smoke.localScale.sqrMagnitude>0,"Actual Animator evaluation animates exhaust in Edit mode");
            var block=new MaterialPropertyBlock(); p.busBody.GetPropertyBlock(block,0);
            check(block.GetColor("_BaseColor")==p.busColor,"Exposed bus colour reaches body material slot only");
            for(int i=0;i<100;i++)p.FitAspect(i%2==0?.5625f:2.37037f);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<2000;i++)p.FitAspect(i%2==0?.5625f:2.37037f);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            check(allocated==0,"2000 warmed camera/sky fits allocate "+allocated+" managed bytes");
            int count=world.GetComponentsInChildren<Transform>(true).Length;
            for(int i=0;i<50;i++){p.ApplyState(GameState.Ready);p.ApplyState(GameState.Start);}
            check(world.GetComponentsInChildren<Transform>(true).Length==count,"50 menu returns reuse world objects");
            int subscribers=((Action<GameState>)publisher.GetValue(p.gameManager)).GetInvocationList().Length;
            for(int i=0;i<10;i++){p.enabled=false;p.enabled=true;}
            check(((Action<GameState>)publisher.GetValue(p.gameManager)).GetInvocationList().Length==subscribers,"Disable/enable keeps subscription count stable");
        }
        finally { p.ApplyState(GameState.Start); p.menuCamera.ResetAspect(); p.RefreshView(); }
        check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)))==0,"No missing scripts");
        text.Insert(0,checks+" ZSCN-3 Edit-mode checks passed.\n"); File.WriteAllText(ZScnMenu.Folder+"/validation.txt",text.ToString()); return text.ToString();
    }
}
#endif
