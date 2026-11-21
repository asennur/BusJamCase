#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using BusJam.Core;
using BusJam.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>Builds the menu from the read-only Blender extraction and preserves the gameplay scene.</summary>
public static class ZScnMenu
{
    public const string Folder = ZScnMenuReview.Folder;
    private const string Art = "Assets/_BusJam/Art/Models/Menu/";
    [Serializable] private class Layout { public Item[] objects, extras; public Shot camera; }
    [Serializable] private class Item { public string name, data, type; public float[] position, quaternion_xyzw, scale; }
    [Serializable] private class Shot { public float[] position, quaternion_xyzw; }
    private static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
    private static Quaternion Q(float[] a) => new Quaternion(a[0], a[1], a[2], a[3]);
    private static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Application.unityVersion != "2022.3.19f1"
            || Application.dataPath != "C:/Users/Kullanici/Documents/BusJamCase/Assets") throw new InvalidOperationException("Pinned compiled Edit mode required.");
    }
    private static Transform Root(string name, Transform parent)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
    }
    private static MeshRenderer Renderer(Transform t, Mesh mesh, Material[] materials)
    {
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = t.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterials = materials;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        return r;
    }
    private static Material Material(string name, string shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(Art+name+".mat");
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, Art+name+".mat"); }
        m.enableInstancing = true; return m;
    }
    private static void CombineScenery(Transform world)
    {
        string generated = Art+"Generated/"; Directory.CreateDirectory(generated); AssetDatabase.Refresh();
        // Two side chunks retain useful landscape culling and share the original palette material.
        for (int side = -1; side <= 1; side += 2)
        {
            var sources = world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Mesh" && r.transform.parent.name!="Menu_Bridge"
                && (r.transform.parent.localPosition.x < 0 ? -1 : 1) == side).ToArray();
            var instances = sources.Select(r=>new CombineInstance { mesh=r.GetComponent<MeshFilter>().sharedMesh,
                transform=world.worldToLocalMatrix*r.transform.localToWorldMatrix }).ToArray();
            var combined = new Mesh { name=side<0?"Menu Left Scenery":"Menu Right Scenery", indexFormat=IndexFormat.UInt32 };
            combined.CombineMeshes(instances,true,true);
            string path = generated+(side<0?"MenuLeft":"MenuRight")+".asset";
            var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (asset==null) { AssetDatabase.CreateAsset(combined,path); asset=combined; }
            else { EditorUtility.CopySerialized(combined,asset); Object.DestroyImmediate(combined); AssetDatabase.SaveAssetIfDirty(asset); }
            Renderer(Root(asset.name,world),asset,new[]{sources[0].sharedMaterial});
            foreach (var source in sources) source.enabled=false;
        }
    }
    public static string Build()
    {
        Guard();
        var data = JsonUtility.FromJson<Layout>(File.ReadAllText(Folder+"/MenuKit-layout.json"));
        var old = GameObject.Find("Menu Presentation"); if (old != null) Object.DestroyImmediate(old);
        foreach (var existingWorld in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Where(g=>g.name=="MenuWorld")) Object.DestroyImmediate(existingWorld);
        var world = Root("MenuWorld", null); world.position = new Vector3(1000, 0, 1000);
        var prototypes = AssetDatabase.LoadAssetAtPath<GameObject>(Art+"MenuKit.fbx").GetComponentsInChildren<MeshFilter>().ToDictionary(m=>m.sharedMesh.name);
        foreach (var item in data.objects.Concat(data.extras.Where(x=>x.name=="Menu_Bridge")))
        {
            var source = prototypes[item.data]; var t = Root(item.name, world);
            t.localPosition = V(item.position); t.localRotation = Q(item.quaternion_xyzw); t.localScale = V(item.scale);
            var mesh = Root("Mesh", t); mesh.localRotation = source.transform.rotation; mesh.localScale = source.transform.lossyScale;
            Renderer(mesh, source.sharedMesh, source.GetComponent<MeshRenderer>().sharedMaterials);
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }
        CombineScenery(world);
        // Keep the preview floor height and fade its distant edge into the backdrop on wide views.
        var ground = Root("Preview Ground",world); ground.localPosition = new Vector3(0,-7,0);
        var floor = new Mesh { name="MenuGround" };
        floor.vertices = new[]{new Vector3(-75,0,50),new Vector3(-75,0,-20),new Vector3(-75,0,-32),new Vector3(75,0,50),new Vector3(75,0,-20),new Vector3(75,0,-32)};
        floor.colors = new[]{Color.white,Color.white,Color.clear,Color.white,Color.white,Color.clear};
        floor.triangles = new[]{0,4,1,0,3,4,1,5,2,1,4,5}; floor.RecalculateBounds(); floor.RecalculateNormals();
        var floorAsset = AssetDatabase.LoadAssetAtPath<Mesh>(Art+"Generated/MenuGround.asset");
        if(floorAsset==null){AssetDatabase.CreateAsset(floor,Art+"Generated/MenuGround.asset");floorAsset=floor;}
        else{EditorUtility.CopySerialized(floor,floorAsset);Object.DestroyImmediate(floor);AssetDatabase.SaveAssetIfDirty(floorAsset);}
        var groundMat = Material("M_MenuGround", "BusJam/Menu Ground"); groundMat.shader=Shader.Find("BusJam/Menu Ground");groundMat.renderQueue=(int)RenderQueue.Transparent;groundMat.SetColor("_BaseColor", new Color(.13f,.29f,.12f));
        Renderer(ground,floorAsset,new[]{groundMat}); EditorUtility.SetDirty(groundMat); AssetDatabase.SaveAssetIfDirty(groundMat);

        var busPose = data.extras.Single(x=>x.name=="Otobus"); var anchor = Root("OtobusMenu",world);
        anchor.localPosition = V(busPose.position); anchor.localRotation = Q(busPose.quaternion_xyzw);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BusJam/Art/Models/Otobus/OtobusMenu.fbx"),anchor);
        model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; model.transform.localScale = Vector3.one / 1.3f;
        var animator = model.GetComponent<Animator>(); if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_BusJam/Art/Models/Otobus/OtobusMenu_Animator.controller");
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>()) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
        var camera = Root("MenuCamera",world).gameObject.AddComponent<Camera>(); camera.transform.localPosition = V(data.camera.position); camera.transform.localRotation = Q(data.camera.quaternion_xyzw);
        camera.nearClipPlane = .3f; camera.farClipPlane = 250; camera.fieldOfView = 52; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f,.6f,.9f);
        camera.allowHDR = true; camera.allowMSAA = true; camera.depth = 0;
        var main = GameObject.Find("Main Camera").GetComponent<Camera>();
        var mainData = main.GetUniversalAdditionalCameraData(); var cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = mainData.renderPostProcessing; cameraData.antialiasing = mainData.antialiasing; cameraData.volumeLayerMask = mainData.volumeLayerMask; cameraData.volumeTrigger = camera.transform;
        var target = Root("Camera Target",world); target.position = camera.transform.position + camera.transform.forward * 32;
        var fill = Root("Menu Fill Light",world).gameObject.AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .85f;
        fill.color = new Color(1,.97f,.9f); fill.transform.rotation = Quaternion.Euler(35,180,0); fill.shadows = LightShadows.None;
        var sky = GameObject.CreatePrimitive(PrimitiveType.Quad); sky.name = "MenuSky Background"; sky.transform.SetParent(camera.transform,false); sky.transform.localPosition = new Vector3(0,0,200);
        Object.DestroyImmediate(sky.GetComponent<Collider>());
        var skyMat = Material("M_MenuSky", "Universal Render Pipeline/Unlit"); skyMat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_BusJam/Art/Textures/MenuSky.png")); skyMat.SetColor("_BaseColor",Color.white);
        sky.GetComponent<MeshRenderer>().sharedMaterial = skyMat; sky.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; sky.GetComponent<MeshRenderer>().receiveShadows = false;
        EditorUtility.SetDirty(skyMat); AssetDatabase.SaveAssetIfDirty(skyMat);
        var presentation = Root("Menu Presentation",null).gameObject.AddComponent<MenuWorldPresentation>();
        presentation.gameManager = Object.FindObjectOfType<GameManager>(); presentation.layout = main.GetComponent<LayoutModeController>();
        presentation.gameplayCamera = main; presentation.menuCamera = camera; presentation.menuWorld = world.gameObject;
        presentation.gameplayCameraData = mainData;
        presentation.cameraTarget = target; presentation.sky = sky.transform; presentation.busAnimator = animator; presentation.busBody = model.GetComponentInChildren<Renderer>();
        presentation.ApplyColor(); presentation.ApplyState(GameState.Start);
        var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/_BusJam/Art/Models/Otobus/OtobusMenu.fbx").OfType<AnimationClip>().Single(c=>c.name=="Idle"); idle.SampleAnimation(model,.6f);
        EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
        string report = "Built "+data.objects.Length+" extracted instances, bridge, preview ground, OtobusMenu, camera and far sky quad.\nCamera target (local): "+target.localPosition+"; pitch="+camera.transform.eulerAngles.x+".\nBus rest bounds="+presentation.busBody.bounds+"; original directional light="+GameObject.Find("Directional Light").GetComponent<Light>().intensity;
        File.WriteAllText(Folder+"/build.txt",report); return report;
    }
    public static string SaveScene()
    {
        Guard();
        if (GameObject.Find("Directional Light").GetComponent<Light>().intensity != .75f) throw new InvalidOperationException("Preserve Asen's original light intensity.");
        string before = File.ReadAllText(Folder+"/Game-before.unity.txt");
        var position = System.Text.RegularExpressions.Regex.Match(before,@"(?s)--- !u!4 &737060412\s+.*?m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}");
        if (!position.Success) throw new InvalidOperationException("Original gameplay camera pose not found.");
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var p = Object.FindObjectOfType<MenuWorldPresentation>();
        p.gameplayCamera.transform.position = new Vector3(float.Parse(position.Groups[1].Value,culture),float.Parse(position.Groups[2].Value,culture),float.Parse(position.Groups[3].Value,culture));
        p.ApplyState(GameState.Start);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Saved menu scene; original directional light intensity stays 0.75.";
    }
}
#endif
