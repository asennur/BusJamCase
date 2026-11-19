using System;
using System.IO;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Imports the three icons and adds only ZART objects to the existing layout.</summary>
    public static class ZARTSetup
    {
        private static UIArt art;
        private static TMP_FontAsset font;
        private static readonly Vector2 Center = Vector2.one * .5f;

        public static string Import(string angryCutout)
        {
            ZM3Setup.RequireEditMode();
            string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            ImportIcon(angryCutout, "icon_angry");
            ImportIcon(Path.Combine(downloads, "Aşağıyı Gösteren Beyaz Çizgi Eldiven.png"), "fx_tap_hand");
            ImportIcon(Path.Combine(downloads, "Parlak Mavi Düğmede Beyaz Dişli.png"), "btn_settings");
            art = AssetDatabase.LoadAssetAtPath<UIArt>("Assets/_BusJam/UI/UIArt.asset");
            art.icon_angry = Sprite("icon_angry");
            art.fx_tap_hand = Sprite("fx_tap_hand");
            art.btn_settings = Sprite("btn_settings");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return "Three RGBA sprites imported, trimmed with 8px padding, max 512, Android/iOS ASTC 6x6, UIArt assigned.";
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_BusJam/Art/UI/" + name + ".png");
        private static void ImportIcon(string source, string name)
        {
            var input = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Texture2D output = null;
            try
            {
                if (!input.LoadImage(File.ReadAllBytes(source))) throw new InvalidOperationException("Invalid icon: " + source);
                var pixels = input.GetPixels32();
                int minX = input.width, minY = input.height, maxX = -1, maxY = -1;
                for (int y = 0; y < input.height; y++)
                    for (int x = 0; x < input.width; x++)
                        if (pixels[y * input.width + x].a > 1)
                        { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
                if (maxX < minX) throw new InvalidOperationException("Empty icon: " + source);
                int width = maxX - minX + 1, height = maxY - minY + 1;
                float scale = Mathf.Min(1f, 496f / Mathf.Max(width, height));
                int w = Mathf.RoundToInt(width * scale), h = Mathf.RoundToInt(height * scale);
                output = new Texture2D(w + 16, h + 16, TextureFormat.RGBA32, false);
                var result = new Color[(w + 16) * (h + 16)];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        result[(y + 8) * (w + 16) + x + 8] = input.GetPixelBilinear(
                            (minX + (x + .5f) * width / w) / input.width,
                            (minY + (y + .5f) * height / h) / input.height);
                output.SetPixels(result); output.Apply();
                string path = "Assets/_BusJam/Art/UI/" + name + ".png";
                File.WriteAllBytes(path, output.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                foreach (string platform in new[] { "Android", "iPhone" })
                    importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    { name = platform, overridden = true, maxTextureSize = 512, format = TextureImporterFormat.ASTC_6x6 });
                importer.SaveAndReimport();
            }
            finally { Object.DestroyImmediate(input); if (output != null) Object.DestroyImmediate(output); }
        }

        public static string Install()
        {
            ZM3Setup.RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/_BusJam/Scenes/Game.unity") throw new InvalidOperationException("Open Game.unity.");
            art = AssetDatabase.LoadAssetAtPath<UIArt>("Assets/_BusJam/UI/UIArt.asset");
            if (art.icon_angry == null || art.fx_tap_hand == null || art.btn_settings == null) throw new InvalidOperationException("Import icons first.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_BusJam/Art/Fonts/Fredoka-Bold SDF.asset");
            var start = ZM3Setup.InScene<StartPanel>(scene);
            var manager = ZM3Setup.InScene<GameManager>(scene);
            var gameplay = ZM3Setup.InScene<GameplayController>(scene);
            if (start.transform.Find("SettingsButton") != null) throw new InvalidOperationException("ZART already installed.");
            var safe = start.transform.parent;
            var settings = Button(start.transform, "SettingsButton", art.btn_settings, "", new Vector2(-92, -70), new Vector2(112, 112));
            var r = (RectTransform)settings.transform;
            r.anchorMin = r.anchorMax = Vector2.one;

            var popup = Rect(safe, "SettingsPopup", Vector2.zero, Vector2.zero);
            Stretch(popup);
            var dim = popup.gameObject.AddComponent<Image>();
            dim.color = new Color(.06f, .03f, .16f, .72f);
            dim.raycastTarget = true;
            popup.gameObject.AddComponent<FullscreenDim>();
            var modal = Image(popup, "panel_modal", art.panel_modal, new Vector2(780, 650), Vector2.zero, true);
            modal.raycastTarget = true;
            var header = Image(modal.transform, "panel_header", art.panel_header, new Vector2(650, 140), new Vector2(0, 220), true);
            Text(header.transform, "Title", "Settings", new Vector2(540, 100), Vector2.zero, 64);
            var close = Button(modal.transform, "Close", art.icon_close, "", new Vector2(315, 245), new Vector2(90, 90));
            var reset = Button(modal.transform, "Reset", art.btn_red, "Reset Progress", new Vector2(0, -40), new Vector2(600, 170));
            var confirm = Rect(modal.transform, "Confirmation", new Vector2(690, 370), new Vector2(0, -40));
            Text(confirm, "Question", "Are you sure?", new Vector2(650, 100), new Vector2(0, 100), 58);
            var yes = Button(confirm, "Yes", art.btn_red, "Yes", new Vector2(-165, -65), new Vector2(290, 145));
            var no = Button(confirm, "No", art.btn_green, "No", new Vector2(165, -65), new Vector2(290, 145));
            var controller = safe.gameObject.AddComponent<SettingsPopup>();
            ZM3Setup.Set(controller, "gameManager", manager);
            ZM3Setup.Set(controller, "settingsButton", settings);
            ZM3Setup.Set(controller, "closeButton", close);
            ZM3Setup.Set(controller, "resetButton", reset);
            ZM3Setup.Set(controller, "yesButton", yes);
            ZM3Setup.Set(controller, "noButton", no);
            ZM3Setup.Set(controller, "popup", popup.gameObject);
            ZM3Setup.Set(controller, "confirmation", confirm.gameObject);
            confirm.gameObject.SetActive(false);
            popup.gameObject.SetActive(false);

            var overlay = Rect(safe, "TapTutorial", Vector2.zero, Vector2.zero);
            Stretch(overlay);
            var anchor = Rect(overlay, "HandAnchor", Vector2.zero, Vector2.zero);
            var hand = Image(anchor, "fx_tap_hand", art.fx_tap_hand, new Vector2(145, 170), Vector2.zero);
            var tip = Image(overlay, "panel_tip", art.panel_tip, new Vector2(980, 150), new Vector2(0, -260), true);
            tip.rectTransform.anchorMin = tip.rectTransform.anchorMax = new Vector2(.5f, 1);
            var hint = Text(tip.transform, "Hint", "", new Vector2(910, 110), Vector2.zero, 30);
            hint.enableWordWrapping = false;
            hint.enableAutoSizing = true; hint.fontSizeMin = 24; hint.fontSizeMax = 30;
            var tutorial = overlay.gameObject.AddComponent<TapTutorial>();
            ZM3Setup.Set(tutorial, "gameManager", manager);
            ZM3Setup.Set(tutorial, "gameplay", gameplay);
            ZM3Setup.Set(tutorial, "worldCamera", ZM3Setup.InScene<Camera>(scene));
            ZM3Setup.Set(tutorial, "overlay", overlay);
            ZM3Setup.Set(tutorial, "handAnchor", anchor);
            ZM3Setup.Set(tutorial, "hand", hand.rectTransform);
            ZM3Setup.Set(tutorial, "tip", tip.gameObject);
            ZM3Setup.Set(tutorial, "hint", hint);
            anchor.gameObject.SetActive(false); tip.gameObject.SetActive(false);
            popup.SetAsLastSibling();
            InstallPassenger();
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(tutorial);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return "Settings, confirmation, tutorial and camera-facing passenger icon saved; existing layout transforms untouched.";
        }
        private static void InstallPassenger()
        {
            const string path = "Assets/_BusJam/Prefabs/Passenger.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var p = root.GetComponent<Passenger>();
                var icon = ZM3Setup.Get<Transform>(p, "angryIcon");
                foreach (var renderer in icon.GetComponents<Renderer>()) Object.DestroyImmediate(renderer);
                var mesh = icon.GetComponent<MeshFilter>(); if (mesh != null) Object.DestroyImmediate(mesh);
                var sprite = icon.gameObject.AddComponent<SpriteRenderer>();
                sprite.sprite = art.icon_angry;
                sprite.sortingOrder = 20;
                icon.localPosition = new Vector3(0, 1.65f, 0);
                icon.localScale = Vector3.one * .14f;
                icon.rotation = Quaternion.Euler(65, 0, 0);
                icon.gameObject.AddComponent<CameraFacingIcon>();
                icon.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = (RectTransform)go.transform;
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = Center;
            r.sizeDelta = size; r.anchoredPosition = position; return r;
        }
        private static void Stretch(RectTransform r)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        private static Image Image(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position, bool sliced = false)
        {
            var r = Rect(parent, name, size, position); var image = r.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false; image.preserveAspect = !sliced;
            image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            if (sliced) image.pixelsPerUnitMultiplier = sprite.rect.height * 100 / (sprite.pixelsPerUnit * size.y);
            return image;
        }
        private static TMP_Text Text(Transform parent, string name, string text, Vector2 size, Vector2 position, float sizeText)
        {
            var r = Rect(parent, name, size, position); var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = sizeText; t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; return t;
        }
        private static Button Button(Transform parent, string name, Sprite sprite, string label, Vector2 pos, Vector2 size)
        {
            var image = Image(parent, name, sprite, size, pos, label.Length > 0); image.raycastTarget = true;
            var b = image.gameObject.AddComponent<Button>(); b.targetGraphic = image;
            if (label.Length > 0) Text(image.transform, "Label", label, size - new Vector2(30, 30), new Vector2(0, 5), 48);
            return b;
        }
    }
}
