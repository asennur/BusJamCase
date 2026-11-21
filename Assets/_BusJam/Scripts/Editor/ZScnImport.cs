#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BusJam.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Explicit ZSCN-1 import and presentation prefab setup; never saves the Game scene.</summary>
public static class ZScnImport
{
    public const string Folder = "C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-1";
    private const string Models = "Assets/_BusJam/Art/Models/";
    private static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Application.unityVersion != "2022.3.19f1"
            || Application.dataPath != "C:/Users/Kullanici/Documents/BusJamCase/Assets") throw new InvalidOperationException("Pinned compiled Edit mode required.");
    }
    private static AnimationClip Clip(string model, string name) => AssetDatabase.LoadAllAssetsAtPath(Models + model + ".fbx").OfType<AnimationClip>().Single(c => c.name == name);
    private static Dictionary<string,long> Ids(string path)
    {
        var ids = new Dictionary<string,long>();
        foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
        { string guid; long id; AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out guid, out id); ids.Add(clip.name,id); }
        return ids;
    }
    private static void Model(string model, float scale, bool animated, System.Text.StringBuilder report)
    {
        string path = Models + model + ".fbx", guid = AssetDatabase.AssetPathToGUID(path);
        var before = Ids(path); var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        var clips = new List<ModelImporterClipAnimation>(importer.clipAnimations);
        foreach (var take in importer.defaultClipAnimations)
        {
            if (clips.Any(c => c.name == take.name)) continue;
            take.loopTime = take.name == "Idle" || take.name == "Drive" || take.name.StartsWith("Cheer_") || take.name == "Celebrate_Hold";
            clips.Add(take);
        }
        importer.globalScale = scale; importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = animated; importer.clipAnimations = animated ? clips.ToArray() : new ModelImporterClipAnimation[0];
        importer.isReadable = model.StartsWith("Yolcu"); importer.optimizeGameObjects = false;
        importer.SaveAndReimport();
        var after = Ids(path);
        if (guid != AssetDatabase.AssetPathToGUID(path) || before.Any(p => !after.ContainsKey(p.Key) || after[p.Key] != p.Value))
            throw new InvalidOperationException("Existing GUID or clip fileID changed: " + path);
        report.AppendLine(path + " GUID=" + guid + " scale=" + scale + " Generic; existing clip IDs preserved");
        foreach (var c in importer.clipAnimations) report.AppendLine("  " + c.name + " take=" + c.takeName + " frames=" + c.firstFrame + ".." + c.lastFrame + " loop=" + c.loopTime + " fileID=" + after[c.name]);
    }
    private static void Controller(string model, string path, bool existing)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach(var child in machine.states)
            if(child.state.name.StartsWith("__preview__")) machine.RemoveState(child.state);
        foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(Models + model + ".fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")))
        {
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == clip.name);
            if (state != null) continue;
            state = machine.AddState(clip.name); state.motion = clip;
            state.speed = clip.name == "Brake" ? 2 : 1;
            if (clip.name == "Idle" && !existing) machine.defaultState = state;
        }
        if(!existing) machine.defaultState=machine.states.Single(s=>s.state.name=="Idle").state;
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
    }
    private static void Texture(string path, bool palette, System.Text.StringBuilder report)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
        importer.filterMode = palette ? FilterMode.Point : FilterMode.Trilinear;
        importer.wrapMode = palette ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.mipmapEnabled = !palette; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048;
        importer.SaveAndReimport(); report.AppendLine(path + " sRGB, uncompressed, " + importer.filterMode + ", " + importer.wrapMode + ", mips=" + importer.mipmapEnabled);
    }
    private static Material Material(string path, string texture, string shader = "Universal Render Pipeline/Simple Lit")
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(material,path); }
        material.shader = Shader.Find(shader); material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        material.SetColor("_BaseColor", Color.white); material.enableInstancing = true;
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); return material;
    }
    private static void Remap(string model, Dictionary<string,Material> materials)
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(Models + model + ".fbx");
        foreach (var pair in materials) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
        importer.SaveAndReimport();
    }
    private static Material PresentationMaterial(Material source, string name)
    {
        string path=Models+"Otobus/Materials/"+name+".mat";
        var copy=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(copy==null){copy=new Material(source);AssetDatabase.CreateAsset(copy,path);}
        copy.CopyPropertiesFromMaterial(source);copy.enableInstancing=true;
        EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);return copy;
    }
    public static string Run()
    {
        Guard(); Directory.CreateDirectory(Folder); var report = new System.Text.StringBuilder();
        foreach (var model in new[]{"Yolcu/Yolcu","Otobus/Otobus","Otobus/OtobusKutlama","Otobus/OtobusMenu"}) Model(model, model.StartsWith("Yolcu") ? 1 : 1.3f, true,report);
        Model("Environment/EnvironmentKit",1,false,report); Model("Menu/MenuKit",1,false,report);
        foreach (var folder in new[]{"Environment","Menu"})
            foreach (var file in Directory.GetFiles(Models + folder,"*.png")) Texture(file.Replace('\\','/'),file.Contains("Palette"),report);
        var sky = (TextureImporter)AssetImporter.GetAtPath("Assets/_BusJam/Art/Textures/MenuSky.png");
        sky.textureType=TextureImporterType.Default; sky.wrapMode=TextureWrapMode.Clamp; sky.filterMode=FilterMode.Bilinear;
        sky.sRGBTexture=true; sky.mipmapEnabled=false; sky.npotScale=TextureImporterNPOTScale.None; sky.maxTextureSize=2048; sky.textureCompression=TextureImporterCompression.Uncompressed; sky.SaveAndReimport();
        var busImporter=(ModelImporter)AssetImporter.GetAtPath(Models+"Otobus/Otobus.fbx");
        var busMaterials=busImporter.GetExternalObjectMap().Where(p=>p.Value is Material).ToDictionary(p=>p.Key.name,p=>(Material)p.Value);
        foreach(var name in busMaterials.Keys.ToArray()) busMaterials[name]=PresentationMaterial(busMaterials[name],name=="M_BusSmoke"?name:"M_Presentation_"+name);
        Remap("Otobus/OtobusKutlama",busMaterials); Remap("Otobus/OtobusMenu",busMaterials);
        var env=Material(Models+"Environment/M_Environment.mat",Models+"Environment/EnvPalette.png");
        var water=Material(Models+"Environment/M_Water.mat",Models+"Environment/EnvWater.png","BusJam/Scrolling Water");
        Remap("Environment/EnvironmentKit",new Dictionary<string,Material>{{"M_EnvPalette",env},{"Water",water}});
        var menu=new Dictionary<string,Material>();
        foreach(var name in new[]{"MenuPalette","MenuAsphalt","MenuWalk"}) menu.Add("M_"+name,Material(Models+"Menu/M_"+name+".mat",Models+"Menu/"+name+".png"));
        Remap("Menu/MenuKit",menu);
        Controller("Yolcu/Yolcu",Models+"Yolcu/Yolcu_Animator.controller",true);
        Controller("Otobus/Otobus",Models+"Otobus/Otobus_Animator.controller",true);
        Controller("Otobus/OtobusKutlama",Models+"Otobus/OtobusKutlama_Animator.controller",false);
        Controller("Otobus/OtobusMenu",Models+"Otobus/OtobusMenu_Animator.controller",false);
        var root = new GameObject("Celebration Bus"); root.SetActive(false);
        try
        {
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models+"Otobus/OtobusKutlama.fbx"),root.transform);
            var view=root.AddComponent<CelebrationVehicle>(); view.model=model.GetComponent<Animator>();view.body=model.GetComponentInChildren<Renderer>(true);
            if(view.model==null) view.model=model.AddComponent<Animator>();
            view.model.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Models+"Otobus/OtobusKutlama_Animator.controller");
            // Cancel each bone's FBX rest orientation while retaining its animated suspension.
            var seats=model.GetComponentsInChildren<Transform>(true);
            view.frontSeat=new GameObject("Front Standing Anchor").transform;
            view.rearSeat=new GameObject("Rear Standing Anchor").transform;
            view.frontSeat.SetParent(seats.Single(t=>t.name=="Seat_0"),false);
            view.rearSeat.SetParent(seats.Single(t=>t.name=="Seat_1"),false);
            view.frontSeat.rotation=view.rearSeat.rotation=Quaternion.identity;
            root.SetActive(true);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/_BusJam/Prefabs/CelebrationBus.prefab");
            var settings=Resources.Load<CelebrationSettings>("CelebrationSettings"); settings.busPrefab=prefab.GetComponent<CelebrationVehicle>();settings.riderCount=2;
            // Dedicated shared copies keep gameplay and the protected colour assets untouched.
            for(int i=0;i<settings.colors.Length;i++)
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/_BusJam/Materials/M_"+((BusJam.Level.PassengerColor)i)+".mat");
                settings.colors[i]=PresentationMaterial(source,"M_Celebration_"+((BusJam.Level.PassengerColor)i));
            }
            settings.drive=Clip("Otobus/OtobusKutlama","Drive");
            settings.celebrateArrive=Clip("Otobus/OtobusKutlama","Celebrate_Arrive");settings.celebrateHold=Clip("Otobus/OtobusKutlama","Celebrate_Hold");settings.celebrateLeave=Clip("Otobus/OtobusKutlama","Celebrate_Leave");
            settings.cheerJump=Clip("Yolcu/Yolcu","Cheer_Jump");settings.cheerLean=Clip("Yolcu/Yolcu","Cheer_Lean");settings.cheerWave=Clip("Yolcu/Yolcu","Cheer_Wave");
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
        }
        finally { Object.DestroyImmediate(root); }
        File.WriteAllText(Folder+"/imports.txt",report.ToString());return report.ToString();
    }
}
#endif
