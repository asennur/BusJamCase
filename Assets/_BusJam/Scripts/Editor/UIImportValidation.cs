using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace BusJam.EditorTools
{
    /// <summary>Enforces native-resolution UI imports and records editor-only atlas costs.</summary>
    public static class UIImportValidation
    {
        private static readonly string[] Roots = { "Assets/_BusJam/Art/UI", "Assets/_BusJam/Art/Textures" };
        private static IEnumerable<string> Paths() => AssetDatabase.FindAssets("t:Texture2D", Roots).Select(AssetDatabase.GUIDToAssetPath);
        private static int MaxSize(int width, int height) => Mathf.Max(width, height) > 2048 ? 4096 : Mathf.NextPowerOfTwo(Mathf.Max(width, height));

        [MenuItem("Bus Jam/Validate UI Import")]
        public static void Validate()
        {
            var failures = new List<string>();
            if (EditorSettings.spritePackerMode == SpritePackerMode.Disabled) failures.Add("Sprite Atlas packing is disabled in Editor Settings.");
            foreach (string path in Paths())
            {
                var i = (TextureImporter)AssetImporter.GetAtPath(path);
                i.GetSourceTextureWidthAndHeight(out int width, out int height);
                int source = Mathf.Max(width, height);
                if (i.maxTextureSize < source || i.mipmapEnabled || i.isReadable || !i.alphaIsTransparency || i.filterMode != FilterMode.Bilinear || i.npotScale != TextureImporterNPOTScale.None)
                    failures.Add(path + ": invalid size/sampling/transparency/readability.");
                foreach (string platform in new[] { "Android", "iPhone", "Standalone" })
                {
                    var p = i.GetPlatformTextureSettings(platform);
                    var required = platform == "Standalone" ? TextureImporterFormat.RGBA32 : TextureImporterFormat.ASTC_4x4;
                    if (!p.overridden || p.maxTextureSize < source || p.format != required || p.compressionQuality != 100 || p.crunchedCompression)
                        failures.Add(path + ": invalid " + platform + " size/format/quality.");
                }
                Vector4 border = i.spriteBorder;
                if (border.x + border.z > width || border.y + border.w > height) failures.Add(path + ": source borders exceed artwork.");
            }
            foreach (string guid in AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets/_BusJam/Art/Atlases" }))
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid));
                var settings = atlas.GetTextureSettings();
                if (settings.generateMipMaps || settings.readable || settings.filterMode != FilterMode.Bilinear)
                    failures.Add(atlas.name + ": invalid atlas sampling/readability.");
                int source = 1;
                foreach (var packable in atlas.GetPackables())
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(packable)) as TextureImporter;
                    if (importer == null) continue;
                    importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                    source = Mathf.Max(source, Mathf.Max(width, height));
                }
                foreach (string platform in new[] { "Android", "iPhone", "Standalone" })
                {
                    var p = atlas.GetPlatformSettings(platform);
                    var format = platform == "Standalone" ? TextureImporterFormat.RGBA32 : TextureImporterFormat.ASTC_4x4;
                    if (!p.overridden || p.maxTextureSize < source || p.format != format || p.compressionQuality != 100 || p.crunchedCompression)
                        failures.Add(atlas.name + ": invalid " + platform + " atlas size/format/quality.");
                }
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
            Debug.Log("UI import validation passed: " + Paths().Count() + " textures; native source size, no mipmaps, ASTC 4x4 / RGBA32.");
        }

        public static string Apply()
        {
            var old = new Dictionary<string, Vector2>();
            var rows = new List<string> { "texture|source px|old max/Android|new max/formats|editor bytes" };
            foreach (string path in Paths())
            {
                var i = (TextureImporter)AssetImporter.GetAtPath(path);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                old[path] = new Vector2(sprite == null ? 1 : sprite.rect.height, sprite == null ? 100 : sprite.pixelsPerUnit);
                i.GetSourceTextureWidthAndHeight(out int width, out int height);
                string before = i.maxTextureSize + "/" + i.GetPlatformTextureSettings("Android").format;
                i.maxTextureSize = MaxSize(width, height); i.mipmapEnabled = false; i.isReadable = false;
                i.filterMode = FilterMode.Bilinear; i.alphaIsTransparency = true; i.npotScale = TextureImporterNPOTScale.None;
                i.textureCompression = TextureImporterCompression.Uncompressed; i.compressionQuality = 100;
                foreach (string platform in new[] { "Android", "iPhone", "Standalone" })
                {
                    var p = i.GetPlatformTextureSettings(platform); p.name = platform; p.overridden = true;
                    p.maxTextureSize = i.maxTextureSize; p.format = platform == "Standalone" ? TextureImporterFormat.RGBA32 : TextureImporterFormat.ASTC_4x4;
                    p.compressionQuality = 100; p.crunchedCompression = false; i.SetPlatformTextureSettings(p);
                }
                i.SaveAndReimport();
                rows.Add(path + "|" + width + "x" + height + "|" + before + "|" + i.maxTextureSize + "/ASTC_4x4/RGBA32|" + (width * (long)height * 4));
            }
            // Compensate imported border scaling, preserving each existing corner's design-pixel thickness.
            foreach (string scene in new[] { "Game", "LevelEditor" })
            {
                var s = EditorSceneManager.OpenScene("Assets/_BusJam/Scenes/" + scene + ".unity");
                foreach (var root in s.GetRootGameObjects())
                    foreach (var image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image.type != Image.Type.Sliced || image.sprite == null) continue;
                        string path = AssetDatabase.GetAssetPath(image.sprite);
                        if (old.TryGetValue(path, out var prior)) image.pixelsPerUnitMultiplier *= image.sprite.rect.height / prior.x * prior.y / image.sprite.pixelsPerUnit;
                    }
                EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s);
            }
            Directory.CreateDirectory("Docs/ZRES-Captures"); File.WriteAllLines("Docs/ZRES-Captures/Imports-After.txt", rows);
            Validate();
            AssetDatabase.SaveAssets();
            return string.Join("\n", rows);
        }

        public static string PackAtlases()
        {
            const string folder = "Assets/_BusJam/Art/Atlases";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_BusJam/Art", "Atlases");
            var atlases = new List<SpriteAtlas>();
            for (int group = 0; group < 2; group++)
            {
                var atlas = new SpriteAtlas();
                atlas.SetPackingSettings(new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4 });
                atlas.SetTextureSettings(new SpriteAtlasTextureSettings { generateMipMaps = false, readable = false, sRGB = true, filterMode = FilterMode.Bilinear });
                foreach (string platform in new[] { "DefaultTexturePlatform", "Android", "iPhone", "Standalone" })
                {
                    var p = atlas.GetPlatformSettings(platform); p.name = platform; p.maxTextureSize = 4096;
                    p.overridden = platform != "DefaultTexturePlatform"; p.compressionQuality = 100;
                    p.format = platform == "Android" || platform == "iPhone" ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.RGBA32;
                    atlas.SetPlatformSettings(p);
                }
                var sprites = AssetDatabase.FindAssets("t:Texture2D", new[] { Roots[0] }).Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(s => s != null && (Mathf.Max(s.rect.width, s.rect.height) > 2048 ? 1 : 0) == group).Cast<UnityEngine.Object>().ToArray();
                atlas.Add(sprites); atlas.SetIncludeInBuild(true);
                AssetDatabase.CreateAsset(atlas, folder + (group == 0 ? "/UI_Icons.spriteatlas" : "/UI_Panels.spriteatlas")); atlases.Add(atlas);
            }
            SpriteAtlasUtility.PackAtlases(atlases.ToArray(), EditorUserBuildSettings.activeBuildTarget, false);
            var result = new List<string>();
            foreach (var atlas in atlases)
                foreach (var texture in (Texture2D[])typeof(SpriteAtlasExtensions).GetMethod("GetPreviewTextures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { atlas }))
                    result.Add(atlas.name + "|" + texture.width + "x" + texture.height + "|ASTC_4x4 bytes=" + (texture.width * (long)texture.height) + "|RGBA32 bytes=" + (texture.width * (long)texture.height * 4));
            File.WriteAllLines("Docs/ZRES-Captures/Atlases.txt", result); AssetDatabase.SaveAssets();
            return string.Join("\n", result);
        }

        public static string RegenerateFonts()
        {
            foreach (string name in new[] { "Fredoka-Bold", "LilitaOne" })
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_BusJam/Art/Fonts/" + (name == "LilitaOne" ? "LilitaOne-Regular" : name) + ".ttf");
                var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_BusJam/Art/Fonts/" + name + " SDF.asset");
                string characters = string.Concat(asset.characterTable.Select(c => char.ConvertFromUtf32((int)c.unicode)));
                var replacement = TMP_FontAsset.CreateFontAsset(font, 96, 14, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
                if (!replacement.TryAddCharacters(characters, out string missing)) throw new InvalidOperationException(name + " missing " + missing);
                replacement.atlasPopulationMode = AtlasPopulationMode.Static;
                var material = asset.material; var texture = asset.atlasTexture; var newMaterial = replacement.material; var newTexture = replacement.atlasTexture;
                var fallbacks = asset.fallbackFontAssetTable;
                EditorUtility.CopySerialized(newTexture, texture); EditorUtility.CopySerialized(replacement, asset);
                texture.name = name + " Atlas"; asset.fallbackFontAssetTable = fallbacks;
                asset.name = name + " SDF"; asset.atlasTextures = new[] { texture }; asset.material = material;
                material.SetTexture("_MainTex", texture); material.SetFloat("_TextureWidth", 2048); material.SetFloat("_TextureHeight", 2048); material.SetFloat("_GradientScale", 15);
                EditorUtility.SetDirty(texture); EditorUtility.SetDirty(material); EditorUtility.SetDirty(asset);
                UnityEngine.Object.DestroyImmediate(replacement); UnityEngine.Object.DestroyImmediate(newMaterial); UnityEngine.Object.DestroyImmediate(newTexture);
            }
            AssetDatabase.SaveAssets(); return "Fredoka-Bold and LilitaOne: sampling 96, padding 14, 2048x2048, original glyph sets and asset GUIDs retained.";
        }
    }
}
