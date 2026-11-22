#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BusJam.EditorTools
{
    /// <summary>Yolun üstündeki çimen dekorunu referans yerleşimine göre kurar: GrassKit modelleri tek palet mesh'i + tek su mesh'i.</summary>
    public static class GrassLayoutBuilder
    {
        private const string Art = "Assets/_BusJam/Art/Models/Environment/";

        // Dünya konumu (x, z), Y dönüşü, X/Z ölçeği (Y = küçük olan). Konumlar referans ekran pikselinden kamera ışınıyla bulundu (Docs/Takip/R6.md).
        private static readonly (string mesh, float x, float z, float yaw, float sx, float sz)[] Props =
        {
            ("Grass_Pond", -6.2f, 20.3f, 0f, 3.0f, 3.0f),
            ("Grass_Fern", -8.7f, 19.0f, 20f, 0.9f, 0.9f),
            ("Grass_Path", -0.4f, 18.4f, 180f, 3.6f, 3.1f),
            ("Grass_Stone_A", -1.2f, 19.6f, 10f, 0.8f, 0.8f),
            ("Grass_Stone_B", -1.4f, 18.5f, 60f, 0.75f, 0.75f),
            ("Grass_Stone_A", -0.6f, 18.6f, 100f, 0.7f, 0.7f),
            ("Grass_Stone_B", 0.2f, 18.8f, 20f, 0.7f, 0.7f),
            ("Grass_Stone_A", -0.5f, 17.8f, 150f, 0.8f, 0.8f),
            ("Grass_Stone_B", 0.1f, 17.2f, 80f, 0.7f, 0.7f),
            ("Grass_Stone_A", -0.6f, 16.5f, 30f, 0.85f, 0.85f),
            ("Grass_Planter", 4.15f, 18.45f, 0f, 1.45f, 1.45f),
            ("Grass_Bush", -2.5f, 20.4f, 0f, 1.3f, 1.3f),
            ("Grass_Tree", 1.6f, 20.7f, 40f, 1.4f, 1.4f),
            ("Grass_Bush", -11.5f, 21.8f, 120f, 1.4f, 1.4f),
            ("Grass_Tree", 9.5f, 22.4f, 200f, 1.7f, 1.7f),
            ("Grass_Bush", 6.5f, 22.0f, 60f, 1.2f, 1.2f),
        };

        public static string Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required.");
            var env = GameObject.Find("Background").transform.Find("Environment");
            foreach (var old in new[] { "Approved Layout", "Grass Layout" })
            {
                var t = env.Find(old);
                if (t != null) Object.DestroyImmediate(t.gameObject);
            }

            var palette = ToonMaterial("M_Environment", "EnvPalette");
            var water = ToonMaterial("M_Water", "EnvWater");
            var kit = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "GrassKit.fbx");
            var meshes = kit.GetComponentsInChildren<MeshFilter>().ToDictionary(f => f.name);

            var dry = new List<CombineInstance>();
            var wet = new List<CombineInstance>();
            foreach (var p in Props)
            {
                var source = meshes[p.mesh];
                var matrix = Matrix4x4.TRS(new Vector3(p.x, -0.3f, p.z), Quaternion.Euler(0, p.yaw, 0), new Vector3(p.sx, Mathf.Min(p.sx, p.sz), p.sz))
                             * kit.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
                for (int i = 0; i < source.sharedMesh.subMeshCount; i++)
                    (i == 0 ? dry : wet).Add(new CombineInstance { mesh = source.sharedMesh, subMeshIndex = i, transform = matrix });
            }

            var root = new GameObject("Grass Layout").transform;
            root.SetParent(env, false);
            Render(root, "GrassLayout", dry, palette);
            Render(root, "GrassWater", wet, water);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            int triangles = root.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
            return "Grass Layout: " + Props.Length + " props, 2 renderers, " + triangles + " triangles";
        }

        // Dekor paleti ve su BusJam/Toon ile çizilir; mevcut materyal asset'i yerinde dönüştürülür (GUID korunur).
        private static Material ToonMaterial(string name, string texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + name + ".mat");
            var map = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + texture + ".png");
            material.shader = Shader.Find("BusJam/Toon");
            material.SetTexture("_BaseMap", map);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void Render(Transform parent, string name, List<CombineInstance> parts, Material material)
        {
            if (parts.Count == 0) return;
            var mesh = new Mesh { name = name };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            string path = Art + "Generated/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
            AssetDatabase.SaveAssetIfDirty(mesh);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }
    }
}
#endif
