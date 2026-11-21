#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BusJam.Core;
using BusJam.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Rebuilds the approved ZSCN-2 scenery from the headless extraction, without touching source models.</summary>
public static class ZScnBackground
{
    public const string Folder="C:/Users/Kullanici/Documents/BusJamCase_ZVEC_Review/scn/zscn-2";
    private const string Art="Assets/_BusJam/Art/Models/Environment/";
    private const string Generated=Art+"Generated/";
    [Serializable] private class Layout { public Item[] objects; }
    [Serializable] private class Item { public string name,data,type; public float[] position,quaternion_xyzw,scale; }
    private static readonly Dictionary<string,MeshFilter> prototypes=new Dictionary<string,MeshFilter>();
    private static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
    private static void Guard()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||Application.unityVersion!="2022.3.19f1"
            ||Application.dataPath!="C:/Users/Kullanici/Documents/BusJamCase/Assets") throw new InvalidOperationException("Pinned compiled Edit mode required.");
    }
    private static Transform Root(string name,Transform parent)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;
    }
    private static Mesh SaveMesh(Mesh mesh,string name)
    {
        mesh.name=name;string path=Generated+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);AssetDatabase.SaveAssetIfDirty(existing);return existing;
    }
    private static MeshRenderer Render(Transform parent,Mesh mesh,Material material)
    {
        parent.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=parent.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        GameObjectUtility.SetStaticEditorFlags(parent.gameObject,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic);
        return renderer;
    }
    private static void Prop(Transform parent,string name,string mesh,Vector3 position,Quaternion rotation,Vector3 scale)
    {
        var prototype=prototypes[mesh];var item=Root(name,parent);
        item.localPosition=position;item.localRotation=rotation;item.localScale=scale;
        var visual=Root("Mesh",item);visual.localRotation=prototype.transform.rotation;visual.localScale=prototype.transform.lossyScale;
        Render(visual,prototype.sharedMesh,prototype.GetComponent<MeshRenderer>().sharedMaterial);
        GameObjectUtility.SetStaticEditorFlags(item.gameObject,StaticEditorFlags.BatchingStatic);
    }
    private static void Combine(Transform parent,string name,Material material,Func<MeshRenderer,bool> choose)
    {
        var renderers=parent.GetComponentsInChildren<MeshRenderer>(true).Where(choose).ToArray();
        if(renderers.Length==0)return;
        var instances=renderers.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,
            transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
        var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(instances,true,true);
        Render(Root(name,parent),SaveMesh(mesh,name),material);
        foreach(var r in renderers)r.enabled=false;
    }
    private static void Ground(Transform root)
    {
        var shader=Shader.Find("BusJam/Modular Ground");if(shader==null)throw new InvalidOperationException("Ground shader must compile first.");
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Art+"M_ModularGround.mat");
        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,Art+"M_ModularGround.mat");}
        mat.enableInstancing=true;EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
        mat.SetColor("_TileLight",new Color(.60f,.52f,.84f));mat.SetColor("_TileDark",new Color(.50f,.44f,.75f));
        mat.SetColor("_Sidewalk",new Color(.80f,.735f,.895f));mat.SetColor("_Road",new Color(.51f,.445f,.78f));
        mat.SetColor("_Grass",new Color(.62f,.88f,.12f));EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
        var vertices=new List<Vector3>();var triangles=new List<int>();var bands=new List<Vector2>();
        Action<float,float,float> band=(near,far,type)=>
        {
            int n=vertices.Count;vertices.Add(new Vector3(-160,-.30f,near));vertices.Add(new Vector3(-160,-.30f,far));
            vertices.Add(new Vector3(160,-.30f,far));vertices.Add(new Vector3(160,-.30f,near));
            for(int i=0;i<4;i++)bands.Add(new Vector2(type,0));triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        };
        // Pixel boundaries on the existing 45-unit quad, measured in the 1254-square source.
        band(-140,4.703254f,0);band(4.703254f,8.650622f,1);band(8.650622f,12.741531f,2);
        band(12.741531f,13.243923f,4);band(13.243923f,140,3);
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(1,bands);mesh.RecalculateNormals();mesh.RecalculateBounds();
        Render(Root("Floor Sidewalk Road Grass",root),SaveMesh(mesh,"GroundBands"),mat);
    }
    public static string Build()
    {
        Guard();Directory.CreateDirectory(Folder);Directory.CreateDirectory(Generated);AssetDatabase.Refresh();
        var original=GameObject.Find("Background");if(original!=null)Object.DestroyImmediate(original);
        prototypes.Clear();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"EnvironmentKit.fbx");
        foreach(var mesh in prefab.GetComponentsInChildren<MeshFilter>())prototypes.Add(mesh.name,mesh);
        var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(Folder+"/EnvironmentKit-layout.json"));
        var background=Root("Background",null);Ground(background);
        var env=Root("Environment",background);var authored=Root("Approved Layout",env);
        float scale=45f/17.0544f;authored.localPosition=new Vector3(0,-.3f,55.0f);authored.localRotation=Quaternion.Euler(0,180,0);authored.localScale=Vector3.one*scale;
        foreach(var item in layout.objects)
        {
            if(item.type!="MESH")continue;var q=item.quaternion_xyzw;
            Prop(authored,item.name,item.data,V(item.position),new Quaternion(q[0],q[1],q[2],q[3]),V(item.scale));
        }
        var floor=Root("Floor Details",env);
        Action<string,string,float,float,float,float> floorProp=(name,mesh,x,y,s,yaw)=>Prop(floor,name,mesh,
            new Vector3((x/1254f-.5f)*45f,-.3f,23.22f-y/1254f*45f),Quaternion.Euler(0,yaw,0),Vector3.one*s);
        floorProp("Left Flower Bush","Env_Bush_Flower",221,983,3.8f,20);floorProp("Left Small Flowers","Env_Bush_Flower",280,1012,1.8f,140);
        floorProp("Right Flower Bush","Env_Bush_Flower",1026,992,3.8f,210);
        floorProp("Left Rock","Env_Rock_A",289,974,3.1f,45);floorProp("Right Rock","Env_Rock_C",952,1006,3.6f,110);
        floorProp("Upper Left Rock","Env_Rock_C",238,558,2.2f,45);floorProp("Upper Right Rock","Env_Rock_A",1021,563,2.5f,80);
        int index=0;foreach(var p in new[]{new Vector2(224,542),new Vector2(1035,540),new Vector2(919,655),new Vector2(703,722),new Vector2(261,863),new Vector2(625,864),new Vector2(780,862),new Vector2(1034,910),new Vector2(191,1016),new Vector2(1064,1019)})
            floorProp("Floor Tuft "+index++,index%2==0?"Env_Grass_A":"Env_Grass_B",p.x,p.y,1.5f,index*71);
        var envMat=AssetDatabase.LoadAssetAtPath<Material>(Art+"M_Environment.mat");
        var water=AssetDatabase.LoadAssetAtPath<Material>(Art+"M_Water.mat");
        Combine(authored,"ApprovedEnvironment",envMat,r=>r.sharedMaterial==envMat);
        Combine(floor,"FloorDetails",envMat,r=>r.sharedMaterial==envMat);
        var wide=Root("Wide Screen Extensions",env);var random=new System.Random(20261121);
        foreach(int side in new[]{-1,1})
        {
            var sideRoot=Root(side<0?"Left":"Right",wide);
            for(int i=0;i<12;i++)
            {
                string mesh=i%4==0?"Env_Tree_"+(char)('A'+random.Next(3)):i%4==1?"Env_Bush_Flower":i%4==2?"Env_Grass_B":"Env_Rock_C";
                float x=side*(27+i*3.6f+(float)random.NextDouble()*1.8f),z=15.8f+(float)random.NextDouble()*7;
                Prop(sideRoot,"Seeded "+i,mesh,new Vector3(x,-.3f,z),Quaternion.Euler(0,random.Next(360),0),Vector3.one*(2.2f+(float)random.NextDouble()*.7f));
            }
            Combine(sideRoot,side<0?"WideLeft":"WideRight",envMat,r=>r.sharedMaterial==envMat);
        }
        var scenery=background.gameObject.AddComponent<BackgroundScenery>();var serialized=new SerializedObject(scenery);
        serialized.FindProperty("layout").objectReferenceValue=Camera.main.GetComponent<LayoutModeController>();
        serialized.FindProperty("wideScenery").objectReferenceValue=wide.gameObject;serialized.ApplyModifiedPropertiesWithoutUndo();
        scenery.enabled=false;scenery.enabled=true;
        GameObject.Find("World Background").transform.Find("Road Sidewalk and Grass").gameObject.SetActive(false);
        var oldCurbs=GameObject.Find("World Background/Curbs");
        foreach(var t in oldCurbs.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.BatchingStatic);
        // The existing curb ends at +/-22.5; continue the same section beyond the portrait composition.
        var curbRenderer=oldCurbs.GetComponentInChildren<MeshRenderer>();var curbMesh=curbRenderer.GetComponent<MeshFilter>().sharedMesh;
        var curbMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"M_ModularCurb.mat");
        if(curbMaterial==null){curbMaterial=new Material(curbRenderer.sharedMaterial);AssetDatabase.CreateAsset(curbMaterial,Art+"M_ModularCurb.mat");}
        curbMaterial.enableInstancing=true;EditorUtility.SetDirty(curbMaterial);AssetDatabase.SaveAssetIfDirty(curbMaterial);
        foreach(var renderer in oldCurbs.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=curbMaterial;
        var curbExt=Root("Curb Extensions",background);
        for(int side=-1;side<=1;side+=2)for(int i=0;i<40;i++)
        {
            var t=Root("Section "+side+" "+i,curbExt);t.position=new Vector3(side*(23.25f+i*1.5f),-.26f,8.58f);
            t.rotation=curbRenderer.transform.rotation;t.localScale=curbRenderer.transform.lossyScale;Render(t,curbMesh,curbMaterial);
        }
        Combine(curbExt,"ExtendedCurbs",curbMaterial,r=>true);
        EditorSceneManager.MarkSceneDirty(background.gameObject.scene);
        var active=background.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
        string report="Built "+layout.objects.Length+" extracted props; 3 floor bushes, 4 rocks, 10 tufts; 24 seeded side props.\n"+
            "Active scenery mesh triangles (including hidden wide extensions): "+active.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3)+"\n"+
            "Layout group: Y=180 degrees; scale="+scale+"; position=(0,-0.3,55).\nOld quad disabled; light="+Object.FindObjectOfType<Light>().intensity;
        File.WriteAllText(Folder+"/build.txt",report);return report;
    }
    public static string SaveScene()
    {
        Guard();if(Mathf.Abs(Object.FindObjectOfType<Light>().intensity-.75f)>0.00001f)throw new InvalidOperationException("Preserve Asen's light intensity.");
        // Edit-mode viewport fitting may dirty the camera by a few millimetres during capture.
        string before=File.ReadAllText(Folder+"/Game-before.unity.txt");
        var block=System.Text.RegularExpressions.Regex.Match(before,@"(?s)--- !u!4 &737060412\s+.*?m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}");
        if(!block.Success)throw new InvalidOperationException("Original camera pose not found.");
        var invariant=System.Globalization.CultureInfo.InvariantCulture;
        Camera.main.transform.position=new Vector3(float.Parse(block.Groups[1].Value,invariant),float.Parse(block.Groups[2].Value,invariant),float.Parse(block.Groups[3].Value,invariant));
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());return "Saved Game with directional intensity 0.75.";
    }
    public static string Validate()
    {
        Guard();var report=new System.Text.StringBuilder();int checks=0;
        Action<bool,string> check=(valid,text)=>{if(!valid)throw new InvalidOperationException(text);checks++;report.AppendLine("PASS "+text);};
        var background=GameObject.Find("Background");var env=background.transform.Find("Environment");var authored=env.Find("Approved Layout");
        var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(Folder+"/EnvironmentKit-layout.json"));
        foreach(var item in layout.objects)
        {
            var t=authored.Find(item.name);var q=item.quaternion_xyzw;
            check(t!=null&&Vector3.Distance(t.localPosition,V(item.position))<.00001f&&Vector3.Distance(t.localScale,V(item.scale))<.00001f
                &&Quaternion.Angle(t.localRotation,new Quaternion(q[0],q[1],q[2],q[3]))<.05f,"Extracted transform "+item.name);
        }
        check(!GameObject.Find("World Background").transform.Find("Road Sidewalk and Grass").gameObject.activeSelf,"Legacy quad inactive");
        check(Mathf.Abs(Object.FindObjectOfType<Light>().intensity-.75f)<.00001f,"Directional intensity 0.75");
        check(background.GetComponentsInChildren<Collider>(true).Length==0,"Scenery has no colliders");
        foreach(var renderer in background.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))
        {
            check(renderer.sharedMaterial.enableInstancing,"Instanced material: "+renderer.name);
            check((GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)&StaticEditorFlags.BatchingStatic)!=0,"Batching static: "+renderer.name);
            check(renderer.shadowCastingMode==ShadowCastingMode.Off&&!renderer.receiveShadows,"No scenery shadows: "+renderer.name);
        }
        foreach(string name in new[]{"EnvPalette","EnvWater"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name+".png");bool palette=name=="EnvPalette";
            check(importer.sRGBTexture&&importer.mipmapEnabled!=palette&&importer.filterMode==(palette?FilterMode.Point:FilterMode.Trilinear)
                &&importer.wrapMode==(palette?TextureWrapMode.Clamp:TextureWrapMode.Repeat),"Texture import "+name);
        }
        var camera=Camera.main;var position=camera.transform.position;var projection=camera.projectionMatrix;var modes=camera.GetComponent<LayoutModeController>();
        try
        {
            foreach(var size in new[]{new Vector2Int(580,1250),new Vector2Int(1080,1920),new Vector2Int(2560,1080),new Vector2Int(1080,1080)})
            {
                camera.aspect=(float)size.x/size.y;modes.ApplyViewport(size.x,size.y,new Rect(0,0,size.x,size.y));camera.GetComponent<CameraFitter>().Fit();
                check(env.Find("Wide Screen Extensions").gameObject.activeSelf==(modes.Mode==LayoutMode.Landscape),"Wide-only activation "+size);
                foreach(var corner in new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.one})
                {
                    var ray=camera.ViewportPointToRay(corner);float distance;new Plane(Vector3.up,new Vector3(0,-.3f,0)).Raycast(ray,out distance);var p=ray.GetPoint(distance);
                    check(Mathf.Abs(p.x)<160&&Mathf.Abs(p.z)<140,"Ground covers frustum "+size+" "+corner);
                }
                foreach(float pixel in new[]{516f,406f,292f,278f})
                {
                    var old=GameObject.Find("World Background").transform.Find("Road Sidewalk and Grass").TransformPoint(new Vector3(0,.5f-pixel/1254,0));
                    var modular=new Vector3(0,-.3f,23.22f-pixel/1254f*45);
                    float error=Vector3.Distance(camera.WorldToScreenPoint(old),camera.WorldToScreenPoint(modular));
                    check(error<.005f,"Projected band error <0.005 px "+size+" source row "+pixel);
                }
            }
        }
        finally {camera.transform.position=position;camera.projectionMatrix=projection;camera.ResetAspect();modes.Refresh(true);}
        int triangles=Object.FindObjectsOfType<MeshRenderer>().Where(r=>r.enabled).Sum(r=>r.GetComponent<MeshFilter>()!=null?r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3:0);
        check(triangles<50000,"Current Edit-mode scene mesh triangles <50000: "+triangles);
        check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)))==0,"No missing scene scripts");
        report.Insert(0,checks+" ZSCN-2 Edit-mode checks passed.\n");File.WriteAllText(Folder+"/validation.txt",report.ToString());return report.ToString();
    }
}
#endif
