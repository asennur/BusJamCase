using System;
using System.IO;
using System.Reflection;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.Save;
using BusJam.UI;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Runs isolated Edit-mode checks and renders two review images without Play.</summary>
    public static class ZARTVerification
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int checks;
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); checks++; }
        private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static T Get<T>(object target, string field) => ZM3Setup.Get<T>(target, field);
        private static void Set(object target, string field, object value) => ZM3Setup.Set(target, field, value);
        private static void Property(object target, string name, object value) => Set(target, "<" + name + ">k__BackingField", value);

        public static string Run()
        {
            ZM3Setup.RequireEditMode(); checks = 0;
            var source = SceneManager.GetActiveScene();
            var preview = EditorSceneManager.NewPreviewScene();
            string[] keys = { "LevelIndex", "Coins", "TutorialSeen_1", "TutorialSeen_6", "TutorialSeen_8", "Lives", "LastRegenUtc", "LivesUtcHighWater" };
            var existed = new bool[keys.Length]; var ints = new int[keys.Length]; var strings = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            { existed[i] = PlayerPrefs.HasKey(keys[i]); if (i < 6) ints[i] = PlayerPrefs.GetInt(keys[i]); else strings[i] = PlayerPrefs.GetString(keys[i]); }
            LevelData level = null, empty = null; LevelDatabase database = null;
            GameObject fixture = null, uiClone = null;
            try
            {
                var art = AssetDatabase.LoadAssetAtPath<UIArt>("Assets/_BusJam/UI/UIArt.asset");
                foreach (string name in new[] { "icon_angry", "fx_tap_hand", "btn_settings" })
                {
                    string path = "Assets/_BusJam/Art/UI/" + name + ".png";
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    Check(importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency && !importer.mipmapEnabled && importer.maxTextureSize == 512, name + " import settings");
                    foreach (string platform in new[] { "Android", "iPhone" })
                    { var p = importer.GetPlatformTextureSettings(platform); Check(p.overridden && p.maxTextureSize == 512 && p.format == TextureImporterFormat.ASTC_6x6, name + " mobile compression"); }
                    var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(path));
                    var pixels = texture.GetPixels32(); int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
                    for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++) if (pixels[y * texture.width + x].a > 1)
                    { minX = Mathf.Min(x, minX); minY = Mathf.Min(y, minY); maxX = Mathf.Max(x, maxX); maxY = Mathf.Max(y, maxY); }
                    Check(minX >= 8 && minY >= 8 && texture.width - maxX - 1 >= 8 && texture.height - maxY - 1 >= 8, name + " alpha padding");
                    Check(texture.width <= 512 && texture.height <= 512 && maxX > minX, name + " dimensions");
                    Object.DestroyImmediate(texture);
                }
                Check(art.icon_angry != null && art.fx_tap_hand != null && art.btn_settings != null, "UIArt references");

                fixture = new GameObject("ZARTChecks"); SceneManager.MoveGameObjectToScene(fixture, preview);
                var manager = fixture.AddComponent<GameManager>(); var lives = fixture.AddComponent<LivesSystem>();
                var timer = fixture.AddComponent<LevelTimer>(); var view = fixture.AddComponent<BoardView>();
                var gameplay = fixture.AddComponent<GameplayController>(); var waiting = fixture.AddComponent<WaitingArea>();
                var station = fixture.AddComponent<BusStation>(); var input = fixture.AddComponent<InputHandler>();
                var board = new Board();
                Property(manager, "Board", board); Set(manager, "livesSystem", lives); Set(manager, "levelTimer", timer); Set(manager, "boardView", view); Set(manager, "inputHandler", input);
                Set(gameplay, "gameManager", manager); Set(gameplay, "board", board); Set(gameplay, "waitingArea", waiting); Set(gameplay, "busStation", station); Set(gameplay, "boardView", view);
                Get<bool[]>(waiting, "unlocked")[1] = true;
                level = ScriptableObject.CreateInstance<LevelData>(); level.width = level.height = 1;
                level.cells = new[] { new CellData { type = CellType.Passenger, color = PassengerColor.Red } };
                board.Setup(level); Set(gameplay, "reachable", new[] { true });
                var prefab = AssetDatabase.LoadAssetAtPath<Passenger>("Assets/_BusJam/Prefabs/Passenger.prefab");
                var passenger = Object.Instantiate(prefab, fixture.transform); Set(passenger, "animator", null); Call(passenger, "Awake");
                passenger.Setup(PassengerColor.Red, Vector2Int.zero, false, Get<Renderer>(passenger, "body").sharedMaterial);
                board.SetOccupant(Vector2Int.zero, passenger);
                var icon = Get<Transform>(passenger, "angryIcon"); var iconScale = icon.localScale;
                Check(icon.GetComponent<SpriteRenderer>().sprite == art.icon_angry && icon.GetComponent<CameraFacingIcon>() != null, "Angry renderer wiring");
                Check(Get<Transform>(passenger, "visualRoot").localScale == Vector3.one * 1.4f, "Passenger 1.4x scale retained");
                passenger.ShowAngry(); var angry = Get<Tween>(passenger, "angryTween"); angry.Pause();
                Check(icon.gameObject.activeSelf && icon.localScale == Vector3.zero, "Angry starts at zero");
                angry.Goto(.096f); Check(Vector3.Distance(icon.localScale, iconScale * 1.1f) < .001f, "Angry pop peak");
                angry.Goto(.192f); Check(Vector3.Distance(icon.localScale, iconScale) < .001f, "Angry pop settles");
                angry.Complete(true); Check(!icon.gameObject.activeSelf, "Angry hides on original timing");

                var sourceSafe = ZM3Setup.InScene<SettingsPopup>(source).transform;
                uiClone = Object.Instantiate(sourceSafe.gameObject); SceneManager.MoveGameObjectToScene(uiClone, preview);
                var popup = uiClone.GetComponent<SettingsPopup>(); Set(popup, "gameManager", manager); Call(popup, "Awake");
                var tutorial = uiClone.GetComponentInChildren<TapTutorial>(true);
                Set(tutorial, "gameManager", manager); Set(tutorial, "gameplay", gameplay); Call(tutorial, "Awake"); Call(tutorial, "OnEnable");
                foreach (int number in new[] { 1, 6, 8 })
                {
                    SaveSystem.ResetProgress(); Property(manager, "CurrentLevelNumber", number); Property(manager, "State", GameState.Playing);
                    passenger.Setup(PassengerColor.Red, Vector2Int.zero, false, Get<Renderer>(passenger, "body").sharedMaterial);
                    board.SetOccupant(Vector2Int.zero, passenger); Array.Clear(Get<Passenger[]>(waiting, "slots"), 0, GameRules.VisibleSlotCount);
                    Get<bool[]>(gameplay, "reachable")[0] = true;
                    Call(tutorial, "Show");
                    Check(Get<RectTransform>(tutorial, "handAnchor").gameObject.activeSelf, "Tutorial shows at " + number);
                    Check((bool)typeof(Tween).GetField("isIndependentUpdate", Private).GetValue(Get<Tween>(tutorial, "tapping")), "Tutorial uses unscaled time");
                    Check(Get<GameObject>(tutorial, "tip").activeSelf == (number != 1), "Tip visibility");
                    if (number != 1) Check(Get<TMP_Text>(tutorial, "hint").text == (number == 6 ? "Free tied passengers by moving their neighbours." : "Cloaked passengers can only move when their colour shows."), "Exact hint");
                    passenger.SetRopes(1); Call(gameplay, "OnPassengerTapped", passenger);
                    Check(!SaveSystem.TutorialSeen(number) && Get<RectTransform>(tutorial, "handAnchor").gameObject.activeSelf, "Rejected tap keeps tutorial");
                    passenger.SetRopes(0); Call(gameplay, "OnPassengerTapped", passenger);
                    Check(SaveSystem.TutorialSeen(number) && !Get<RectTransform>(tutorial, "handAnchor").gameObject.activeSelf, "Accepted tap persists dismissal");
                    Get<Tween>(passenger, "runTween")?.Kill();
                    passenger.Setup(PassengerColor.Red, Vector2Int.zero, false, Get<Renderer>(passenger, "body").sharedMaterial); board.SetOccupant(Vector2Int.zero, passenger);
                    Call(tutorial, "Show"); Check(!Get<RectTransform>(tutorial, "handAnchor").gameObject.activeSelf, "Seen tutorial stays hidden");
                }
                Property(manager, "CurrentLevelNumber", 2); Call(tutorial, "Show");
                Check(!Get<RectTransform>(tutorial, "handAnchor").gameObject.activeSelf, "No tutorial on ordinary levels");
                Call(tutorial, "OnDisable"); Get<Tween>(tutorial, "tapping").Kill();

                empty = ScriptableObject.CreateInstance<LevelData>(); empty.width = empty.height = 1; empty.cells = new[] { new CellData { type = CellType.Void } };
                database = ScriptableObject.CreateInstance<LevelDatabase>(); database.Add(empty); Set(manager, "levelDatabase", database);
                SaveSystem.SetLevelIndex(7); SaveSystem.AddCoins(123); foreach (int number in new[] { 1, 6, 8 }) SaveSystem.CompleteTutorial(number);
                foreach (var counter in uiClone.GetComponentsInChildren<CoinCounter>(true)) Call(counter, "OnEnable");
                lives.ResetFull(); lives.TrySpendLife(); Set(manager, "currentLevelIndex", 7); Property(manager, "State", GameState.Start);
                Get<Button>(popup, "settingsButton").onClick.Invoke(); Check(Get<GameObject>(popup, "popup").activeSelf, "Settings opens");
                Get<Button>(popup, "resetButton").onClick.Invoke(); Check(Get<GameObject>(popup, "confirmation").activeSelf && SaveSystem.GetLevelIndex() == 7, "Reset requires confirmation");
                Get<Button>(popup, "noButton").onClick.Invoke(); Check(!Get<GameObject>(popup, "confirmation").activeSelf && SaveSystem.GetLevelIndex() == 7, "No preserves save");
                Get<Button>(popup, "resetButton").onClick.Invoke(); Get<Button>(popup, "yesButton").onClick.Invoke();
                Check(SaveSystem.GetLevelIndex() == 0 && manager.CurrentLevelNumber == 1 && manager.State == GameState.Start, "Confirmed reset loads Level 1");
                Check(lives.Lives == lives.MaxLives && lives.NextLifeSeconds == 0 && SaveSystem.Coins == 0, "Reset refills lives and clears coins");
                foreach (int number in new[] { 1, 6, 8 }) Check(!SaveSystem.TutorialSeen(number), "Reset clears tutorial " + number);
                Check(!Get<GameObject>(popup, "popup").activeSelf, "Reset closes popup");
                foreach (var counter in uiClone.GetComponentsInChildren<CoinCounter>(true))
                    Check(Get<TMP_Text>(counter, "countText").text == "0", "Visible coin counters refresh after reset");
                return checks + " Edit-mode checks passed: imports/alpha, 1.4x icon/pop/timing, rejected/accepted taps on 1/6/8, saved dismissal, settings Yes/No and complete progress reset. Play remained off.";
            }
            finally
            {
                if (uiClone != null)
                {
                    var tutorial = uiClone.GetComponentInChildren<TapTutorial>(true);
                    if (tutorial != null) Get<Tween>(tutorial, "tapping")?.Kill();
                    foreach (var counter in uiClone.GetComponentsInChildren<CoinCounter>(true)) Call(counter, "OnDisable");
                    Object.DestroyImmediate(uiClone);
                }
                if (fixture != null)
                {
                    foreach (var p in fixture.GetComponentsInChildren<Passenger>(true))
                        foreach (string field in new[] { "runTween", "angryTween", "revealTween", "strideTween", "armTween", "cloakTween" }) Get<Tween>(p, field)?.Kill();
                    Object.DestroyImmediate(fixture);
                }
                EditorSceneManager.ClosePreviewScene(preview);
                if (level != null) Object.DestroyImmediate(level); if (empty != null) Object.DestroyImmediate(empty); if (database != null) Object.DestroyImmediate(database);
                for (int i = 0; i < keys.Length; i++)
                { if (!existed[i]) PlayerPrefs.DeleteKey(keys[i]); else if (i < 6) PlayerPrefs.SetInt(keys[i], ints[i]); else PlayerPrefs.SetString(keys[i], strings[i]); }
                PlayerPrefs.Save();
            }
        }

        public static string Capture(bool settings)
        {
            ZM3Setup.RequireEditMode();
            var source = SceneManager.GetActiveScene(); var preview = EditorSceneManager.NewPreviewScene();
            var rt = new RenderTexture(1080, 1920, 24); var old = RenderTexture.active; Texture2D result = null;
            try
            {
                var cameraObject = new GameObject("ZARTCamera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject, preview);
                var camera = cameraObject.GetComponent<Camera>(); camera.scene = preview; camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true; camera.orthographicSize = 960; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f, .16f, .3f); camera.targetTexture = rt;
                foreach (var root in source.GetRootGameObjects())
                {
                    if (root.GetComponent<Canvas>() == null) continue;
                    var clone = Object.Instantiate(root); SceneManager.MoveGameObjectToScene(clone, preview);
                    var canvas = clone.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = canvas.sortingOrder < 0 ? 20 : 10;
                    var safe = clone.GetComponentInChildren<SafeArea>(true); if (safe == null) continue;
                    foreach (var b in clone.GetComponentsInChildren<MonoBehaviour>(true)) if (b is SafeArea || b is FullscreenDim) b.enabled = false;
                    foreach (var panel in new[] { "StartPanel", "HudPanel", "CompletePanel", "FailPanel", "NoLivesPanel", "CoinCounter" })
                    { var child = safe.transform.Find(panel); if (child != null) child.gameObject.SetActive(panel == "StartPanel"); }
                    var popup = clone.GetComponentInChildren<SettingsPopup>(true); Get<GameObject>(popup, "popup").SetActive(settings);
                    var safeRect = (RectTransform)safe.transform; safeRect.anchorMin = new Vector2(0, .04f); safeRect.anchorMax = new Vector2(1, .96f); safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
                    if (!settings) continue;
                }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
                result = new Texture2D(1080, 1920, TextureFormat.RGB24, false); result.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); result.Apply();
                Directory.CreateDirectory("Docs/ZART-Captures"); string path = "Docs/ZART-Captures/" + (settings ? "Settings" : "Start") + ".png";
                File.WriteAllBytes(path, result.EncodeToPNG()); return path;
            }
            finally { RenderTexture.active = old; if (result != null) Object.DestroyImmediate(result); EditorSceneManager.ClosePreviewScene(preview); rt.Release(); Object.DestroyImmediate(rt); }
        }
    }
}
