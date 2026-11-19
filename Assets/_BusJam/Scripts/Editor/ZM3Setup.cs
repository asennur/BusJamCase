using System;
using System.IO;
using System.Reflection;
using BusJam.Core;
using BusJam.Save;
using BusJam.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Builds and captures the lives UI in Edit mode without entering Play.</summary>
    public static class ZM3Setup
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static TMP_FontAsset font;
        private static readonly Color Ink = new Color(0.09f, 0.16f, 0.3f);

        internal static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);

        internal static T Get<T>(object target, string field) =>
            (T)target.GetType().GetField(field, Private).GetValue(target);

        internal static T InScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            throw new InvalidOperationException(typeof(T).Name + " is missing.");
        }

        public static string Build()
        {
            RequireEditMode();
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/_BusJam/Scenes/Game.unity") throw new InvalidOperationException("Open Game.unity first.");
            var manager = InScene<GameManager>(scene);
            var ui = InScene<UIManager>(scene);
            var hud = InScene<HudPanel>(scene);
            var start = InScene<StartPanel>(scene);
            var art = AssetDatabase.LoadAssetAtPath<UIArt>("Assets/_BusJam/UI/UIArt.asset");
            font = TMP_Settings.defaultFontAsset;
            var fonts = AssetDatabase.FindAssets("Fredoka t:TMP_FontAsset");
            if (fonts.Length > 0) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fonts[0]));
            var lives = manager.transform.GetComponentInChildren<LivesSystem>(true);
            if (lives == null)
            {
                var go = new GameObject("LivesSystem");
                go.transform.SetParent(manager.transform, false);
                lives = go.AddComponent<LivesSystem>();
            }
            Set(lives, "maxLives", 5);
            Set(lives, "regenMinutes", 20);
            Set(manager, "livesSystem", lives);
            Set(ui, "livesSystem", lives);

            Counter(hud.transform, lives, art);
            Counter(start.transform, lives, art);
            var levelText = Get<TMP_Text>(hud, "levelText");
            Place(levelText.rectTransform, new Vector2(0, 1), new Vector2(180, -94), new Vector2(280, 84));
            levelText.alignment = TextAlignmentOptions.Center;
            levelText.fontSize = 56;
            var timer = Get<TMP_Text>(hud, "timeText");
            Place(timer.rectTransform, new Vector2(1, 1), new Vector2(-142, -94), new Vector2(210, 84));
            timer.alignment = TextAlignmentOptions.Center;
            timer.fontSize = 56;

            var restart = ImageAt(hud.transform, "btn_round_blue", art.btn_round_blue,
                new Vector2(1, 0), new Vector2(-102, 160), new Vector2(140, 140));
            restart.raycastTarget = true;
            var button = restart.GetComponent<Button>() ?? restart.gameObject.AddComponent<Button>();
            button.targetGraphic = restart;
            var colors = button.colors;
            colors.pressedColor = new Color(0.7f, 0.85f, 1f);
            button.colors = colors;
            ImageAt(restart.transform, "icon_restart", art.icon_restart, Vector2.one * 0.5f, Vector2.zero, new Vector2(112, 112));
            var label = TextAt(restart.transform, "RestartLabel", "Restart", Vector2.one * 0.5f,
                new Vector2(0, -102), new Vector2(190, 60), 36, Color.white);
            label.fontStyle = FontStyles.Bold;
            Set(hud, "restartButton", button);

            var noLives = Child(ui.transform, "NoLivesPanel");
            Stretch(noLives);
            var dim = noLives.GetComponent<Image>() ?? noLives.gameObject.AddComponent<Image>();
            dim.color = new Color(0.06f, 0.03f, 0.16f, 0.72f);
            dim.raycastTarget = true;
            var modal = ImageAt(noLives, "panel_modal", art.panel_modal, Vector2.one * 0.5f,
                Vector2.zero, new Vector2(780, 740), true);
            var header = ImageAt(modal.transform, "panel_header", art.panel_header, Vector2.one * 0.5f,
                new Vector2(0, 244), new Vector2(660, 144), true);
            TextAt(header.transform, "Title", "No lives", Vector2.one * 0.5f, Vector2.zero, new Vector2(600, 100), 64, Color.white);
            ImageAt(modal.transform, "icon_heart", art.icon_heart, Vector2.one * 0.5f,
                new Vector2(-50, 76), new Vector2(186, 172));
            var count = TextAt(modal.transform, "LivesCount", "0", Vector2.one * 0.5f,
                new Vector2(100, 70), new Vector2(100, 110), 76, Color.white);
            var group = Child(modal.transform, "Countdown");
            Place(group, Vector2.one * 0.5f, new Vector2(0, -105), new Vector2(690, 100));
            ImageAt(group, "icon_clock", art.icon_clock, Vector2.one * 0.5f,
                new Vector2(-244, 0), new Vector2(80, 86));
            var countdown = TextAt(group, "CountdownText", "Next life in 20:00", Vector2.one * 0.5f,
                new Vector2(45, 0), new Vector2(510, 80), 44, Color.white);
            TextAt(modal.transform, "Hint", "Lives refill automatically.", Vector2.one * 0.5f,
                new Vector2(0, -210), new Vector2(650, 80), 38, Color.white);
            BindDisplay(noLives, lives, count, countdown, group.gameObject, true);
            Set(ui, "noLivesPanel", noLives.gameObject);
            noLives.gameObject.SetActive(false);
            hud.gameObject.SetActive(false);
            start.gameObject.SetActive(true);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(ui);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(lives);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "LivesSystem, HUD/Start counters, Restart and NoLivesPanel saved; production regeneration is 20 minutes.";
        }

        private static void Counter(Transform parent, LivesSystem lives, UIArt art)
        {
            var root = Child(parent, "LivesCounter");
            Place(root, new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(270, 112));
            var plateSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_BusJam/Art/UI/counter_plate.png");
            var plate = ImageAt(root, plateSprite == null ? "panel_header" : "counter_plate",
                plateSprite == null ? art.panel_header : plateSprite, Vector2.one * 0.5f, Vector2.zero, new Vector2(270, 112), true);
            plate.transform.SetAsFirstSibling();
            ImageAt(root, "icon_heart", art.icon_heart, Vector2.one * 0.5f,
                new Vector2(-66, 0), new Vector2(100, 92));
            var count = TextAt(root, "LivesCount", "5", Vector2.one * 0.5f, new Vector2(50, 0), new Vector2(100, 84), 62, Color.white);
            var group = Child(root, "Countdown");
            Place(group, Vector2.one * 0.5f, new Vector2(0, -86), new Vector2(270, 62));
            var shade = ImageAt(group, "panel_header", art.panel_header, Vector2.one * 0.5f, Vector2.zero, new Vector2(270, 62), true);
            shade.transform.SetAsFirstSibling();
            ImageAt(group, "icon_clock", art.icon_clock, Vector2.one * 0.5f,
                new Vector2(-84, 0), new Vector2(48, 52));
            var time = TextAt(group, "CountdownText", "20:00", Vector2.one * 0.5f,
                new Vector2(24, 0), new Vector2(160, 56), 38, Color.white);
            BindDisplay(root, lives, count, time, group.gameObject, false);
            group.gameObject.SetActive(false);
        }

        private static void BindDisplay(Transform root, LivesSystem lives, TMP_Text count, TMP_Text time, GameObject group, bool label)
        {
            var display = root.GetComponent<LivesDisplay>() ?? root.gameObject.AddComponent<LivesDisplay>();
            Set(display, "livesSystem", lives);
            Set(display, "countText", count);
            Set(display, "countdownText", time);
            Set(display, "countdownGroup", group);
            Set(display, "showNextLifeLabel", label);
            EditorUtility.SetDirty(display);
        }

        private static RectTransform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return (RectTransform)child;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image ImageAt(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size, bool sliced = false)
        {
            var rect = Child(parent, name);
            Place(rect, anchor, position, size);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text TextAt(Transform parent, string name, string text, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var rect = Child(parent, name);
            Place(rect, anchor, position, size);
            var label = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Truncate;
            return label;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode is required.");
        }

        public static string Capture(string panel, int width = 1080, int height = 1920)
        {
            RequireEditMode();
            var source = SceneManager.GetActiveScene();
            var preview = EditorSceneManager.NewPreviewScene();
            var texture = new RenderTexture(width, height, 24);
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                var cameraObject = new GameObject("CaptureCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, preview);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = preview;
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = height / 2f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Ink;
                camera.targetTexture = texture;
                foreach (var root in source.GetRootGameObjects())
                {
                    if (root.GetComponent<Canvas>() == null) continue;
                    var clone = Object.Instantiate(root);
                    SceneManager.MoveGameObjectToScene(clone, preview);
                    var canvas = clone.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = canvas.sortingOrder < 0 ? 20 : 10;
                    var safe = clone.transform.Find("SafeArea");
                    if (safe == null) continue;
                    foreach (string name in new[] { "StartPanel", "HudPanel", "CompletePanel", "FailPanel", "NoLivesPanel" })
                        safe.Find(name).gameObject.SetActive(name == panel);
                    foreach (var display in safe.GetComponentsInChildren<LivesDisplay>(true))
                        typeof(LivesDisplay).GetMethod("Show", Private).Invoke(display, new object[] { panel == "NoLivesPanel" ? 0 : 3, 754 });
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = texture;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Docs/ZM3-Captures");
                string path = $"Docs/ZM3-Captures/{panel}-{width}x{height}.png";
                File.WriteAllBytes(path, image.EncodeToPNG());
                return path;
            }
            finally
            {
                RenderTexture.active = previous;
                if (image != null) Object.DestroyImmediate(image);
                EditorSceneManager.ClosePreviewScene(preview);
                Object.DestroyImmediate(texture);
            }
        }

        public static string PreparePlayTest()
        {
            RequireEditMode();
            var scene = SceneManager.GetActiveScene();
            var lives = InScene<LivesSystem>(scene);
            Set(lives, "maxLives", 5);
            Set(lives, "regenMinutes", 1);
            EditorUtility.SetDirty(lives);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SaveSystem.SetLevelIndex(0);
            PlayerPrefs.SetInt("Lives", 1);
            string now = DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PlayerPrefs.SetString("LastRegenUtc", now);
            PlayerPrefs.SetString("LivesUtcHighWater", now);
            PlayerPrefs.Save();
            return "Uncommitted setup: regeneration 1 minute, Lives 1, LevelIndex 0 (Level_01, 30 seconds). Play is off.";
        }

        [MenuItem("Bus Jam/ZM3/Prepare One-Life Test (Edit)")]
        private static void PrepareMenu() => Debug.Log(PreparePlayTest());

        [MenuItem("Bus Jam/ZM3/Reset To Five Lives (Edit)")]
        private static void FiveLivesMenu()
        {
            RequireEditMode();
            PlayerPrefs.SetInt("Lives", 5);
            string now = DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PlayerPrefs.SetString("LastRegenUtc", now);
            PlayerPrefs.SetString("LivesUtcHighWater", now);
            PlayerPrefs.Save();
        }

        [MenuItem("Bus Jam/ZM3/Fail Current Run (Play)")]
        private static void FailMenu()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Start Play manually first.");
            InScene<GameManager>(SceneManager.GetActiveScene()).Fail(FailReason.OutOfTime);
        }

        [MenuItem("Bus Jam/ZM3/Restore Production (Edit)")]
        private static void RestoreMenu() => Debug.Log(RestoreProduction());

        public static string RestoreProduction()
        {
            RequireEditMode();
            var scene = SceneManager.GetActiveScene();
            var lives = InScene<LivesSystem>(scene);
            Set(lives, "maxLives", 5);
            Set(lives, "regenMinutes", 20);
            EditorUtility.SetDirty(lives);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SaveSystem.SetLevelIndex(49);
            PlayerPrefs.DeleteKey("Lives");
            PlayerPrefs.DeleteKey("LastRegenUtc");
            PlayerPrefs.DeleteKey("LivesUtcHighWater");
            PlayerPrefs.Save();
            return "Production restored: regeneration 20 minutes, level index 49, fresh default of 5 lives.";
        }

        [MenuItem("Bus Jam/Lives/Clear Lives Data (Edit)")]
        public static void ClearLivesData()
        {
            RequireEditMode();
            PlayerPrefs.DeleteKey("Lives");
            PlayerPrefs.DeleteKey("LastRegenUtc");
            PlayerPrefs.DeleteKey("LivesUtcHighWater");
            PlayerPrefs.Save();
        }
    }
}
