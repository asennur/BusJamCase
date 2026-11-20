using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BusJam.EditorTools
{
    /// <summary>Applies the shadow-free mobile lighting preset without moving gameplay or UI objects.</summary>
    public static class ZLightSetup
    {
        private const string ProfilePath = "Assets/_BusJam/Settings/PP_Game.asset";

        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required.");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/_BusJam/Settings/URP-Mobile.asset");
            var serialized = new SerializedObject(pipeline);
            serialized.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            serialized.FindProperty("m_SoftShadowsSupported").boolValue = false;
            serialized.FindProperty("m_AnyShadowsSupported").boolValue = false;
            serialized.FindProperty("m_ShadowDistance").floatValue = 0;
            serialized.FindProperty("m_SupportsHDR").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var tone = Effect<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.None);
            var colour = Effect<ColorAdjustments>(profile);
            colour.postExposure.Override(.15f);
            colour.contrast.Override(12);
            colour.saturation.Override(20);
            var balance = Effect<WhiteBalance>(profile);
            balance.temperature.Override(10);
            balance.tint.Override(2);
            var bloom = Effect<Bloom>(profile);
            bloom.threshold.Override(.9f);
            bloom.intensity.Override(.35f);
            bloom.scatter.Override(.6f);
            bloom.highQualityFiltering.Override(false);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);
            bloom.maxIterations.Override(3);
            bloom.dirtIntensity.Override(0);
            var vignette = Effect<Vignette>(profile);
            vignette.color.Override(new Color(.14f, .075f, .045f));
            vignette.intensity.Override(.18f);
            vignette.smoothness.Override(.4f);
            vignette.rounded.Override(false);
            EditorUtility.SetDirty(profile);
            foreach (var component in profile.components) EditorUtility.SetDirty(component);

            int prefabRenderers = 0, sceneRenderers = 0, materials = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    if (DisableShadow(renderer)) { changed = true; prefabRenderers++; }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.62f, .59f, .53f);
                RenderSettings.ambientEquatorColor = new Color(.48f, .46f, .42f);
                RenderSettings.ambientGroundColor = new Color(.30f, .275f, .24f);
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                        if (DisableShadow(renderer))
                        {
                            sceneRenderers++;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                        }
                    foreach (var light in root.GetComponentsInChildren<Light>(true))
                    {
                        light.shadows = LightShadows.None;
                        if (light.type != LightType.Directional) continue;
                        light.color = new Color(1, 240f / 255, 215f / 255);
                        light.intensity = 1.15f;
                        light.transform.rotation = Quaternion.Euler(42, -20, 0);
                    }
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        camera.allowHDR = false;
                        var data = camera.GetUniversalAdditionalCameraData();
                        data.renderPostProcessing = true;
                        data.antialiasing = AntialiasingMode.None;
                        data.dithering = false;
                        data.stopNaN = false;
                        data.volumeLayerMask = 1;
                    }
                }
                var volumes = UnityEngine.Object.FindObjectsOfType<Volume>(true);
                Volume volume;
                if (volumes.Length == 0) volume = new GameObject("Global Volume").AddComponent<Volume>();
                else volume = volumes[0];
                volume.isGlobal = true;
                volume.priority = 0;
                volume.weight = 1;
                volume.sharedProfile = profile;
                for (int i = 1; i < volumes.Length; i++) volumes[i].enabled = false;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            foreach (string name in new[] { "M_Red", "M_Pink", "M_Blue", "M_Green", "M_White", "M_BusBody" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BusJam/Materials/" + name + ".mat");
                material.SetFloat("_Smoothness", .08f);
                material.SetFloat("_SpecularHighlights", 1);
                material.SetColor("_SpecColor", Color.black);
                material.DisableKeyword("_SPECGLOSSMAP");
                material.DisableKeyword("_SPECULAR_COLOR");
                material.DisableKeyword("_GLOSSINESS_FROM_BASE_ALPHA");
                if (name == "M_Pink")
                {
                    var pink = material.GetColor("_BaseColor");
                    pink.g = .478f;
                    material.SetColor("_BaseColor", pink);
                    material.SetColor("_Color", pink);
                }
                EditorUtility.SetDirty(material);
                materials++;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/_BusJam/Scenes/Game.unity", OpenSceneMode.Single);
            return "Applied ZLIGHT: " + prefabRenderers + " prefab renderers, " + sceneRenderers + " scene renderers, " + materials + " matte materials; HDR off.";
        }

        private static T Effect<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var effect)) return effect;
            effect = profile.Add<T>(false);
            AssetDatabase.AddObjectToAsset(effect, profile);
            return effect;
        }

        private static bool DisableShadow(Renderer renderer)
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) return false;
            if (renderer.shadowCastingMode == ShadowCastingMode.Off && !renderer.receiveShadows) return false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            EditorUtility.SetDirty(renderer);
            return true;
        }
    }
}
