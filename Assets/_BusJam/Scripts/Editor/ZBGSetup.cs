using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BusJam.Gameplay;
using BusJam.Level;
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
    /// <summary>Imports ZBG art and captures temporary fixtures in the actual Edit-mode Game view.</summary>
    public static class ZBGSetup
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string Fonts = "Assets/_BusJam/Art/Fonts/";
        private static Action cleanup;
        private static string captureResult;

        // Advance only this helper's callbacks when an unfocused editor defers delayCall.
        public static string CaptureStatus()
        {
            var pending = EditorApplication.delayCall;
            if (cleanup != null && pending != null)
                foreach (var callback in pending.GetInvocationList().Cast<EditorApplication.CallbackFunction>()
                    .Where(d => d.Method.DeclaringType.FullName.Contains(typeof(ZBGSetup).FullName)))
                {
                    EditorApplication.delayCall -= callback;
                    callback();
                }
            return captureResult;
        }

        public static string ImportArt()
        {
            ZM3Setup.RequireEditMode();
            ImportSprite("Assets/_BusJam/Art/Textures/bg_game.png", 2048, Vector4.zero);
            ImportSprite("Assets/_BusJam/Art/Textures/bg_menu.png", 2048, Vector4.zero);
            ImportSprite("Assets/_BusJam/Art/UI/counter_plate.png", 1024, new Vector4(400, 140, 400, 140));
            var art = AssetDatabase.LoadAssetAtPath<UIArt>("Assets/_BusJam/UI/UIArt.asset");
            art.counter_plate = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_BusJam/Art/UI/counter_plate.png");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            return "Three sprite importers and UIArt.counter_plate saved.";
        }

        private static void ImportSprite(string path, int maxSize, Vector4 border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 50;
            importer.spriteBorder = border;
            foreach (string platform in new[] { "Android", "iPhone" })
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform, overridden = true, maxTextureSize = maxSize,
                    format = TextureImporterFormat.ASTC_6x6, compressionQuality = 50
                });
            importer.SaveAndReimport();
        }

        public static string CreateFont(string source, string name)
        {
            ZM3Setup.RequireEditMode();
            string path = Fonts + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) return "Already exists: " + path;
            var font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(Fonts + source),
                64, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            font.name = name;
            var characters = Enumerable.Range(32, 95).Concat(Enumerable.Range(160, 96)).Select(c => (uint)c).ToArray();
            font.TryAddCharacters(characters, out uint[] missing);
            if (name == "LilitaOne SDF")
                font.fallbackFontAssetTable.Add(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fonts + "Fredoka-Bold SDF.asset"));
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.material.name = name + " Material";
            font.material.shader = Shader.Find("TextMeshPro/Distance Field");
            font.material.SetFloat("_OutlineWidth", 0.2f);
            font.material.SetColor("_OutlineColor", new Color(0.025f, 0.04f, 0.1f, 1));
            font.material.EnableKeyword("UNDERLAY_ON");
            font.material.SetColor("_UnderlayColor", new Color(0.015f, 0.025f, 0.07f, 0.65f));
            font.material.SetFloat("_UnderlayOffsetX", 0.25f);
            font.material.SetFloat("_UnderlayOffsetY", -0.35f);
            font.material.SetFloat("_UnderlaySoftness", 0.2f);
            AssetDatabase.CreateAsset(font, path);
            font.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return name + ": " + font.characterTable.Count + " characters; missing " + string.Join(",", missing ?? Array.Empty<uint>());
        }

        public static string ApplyScene()
        {
            ZM3Setup.RequireEditMode();
            var scene = SceneManager.GetActiveScene();
            var camera = ZM3Setup.InScene<Camera>(scene);
            camera.transform.SetPositionAndRotation(new Vector3(0, 25.37662f, -13.83331f), Quaternion.Euler(65, 0, 0));
            camera.fieldOfView = 40;
            ZM3Setup.InScene<BoardView>(scene).transform.position = new Vector3(0, 0, -1.5f);
            ZM3Setup.InScene<WaitingArea>(scene).transform.position = new Vector3(0, 0, 1.2f);
            ZM3Setup.Get<Transform>(ZM3Setup.InScene<BusStation>(scene), "doorPoint").position = new Vector3(0, 0, 4.2f);
            var background = scene.GetRootGameObjects().First(g => g.name == "Background").GetComponent<Canvas>();
            var game = background.GetComponentInChildren<Image>();
            game.name = "bg_game";
            game.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_BusJam/Art/Textures/bg_game.png");
            FitArtwork(game, (RectTransform)background.transform, new Rect(0, -0.3f, 1, 1.3f));

            var ui = ZM3Setup.InScene<UIManager>(scene);
            var start = ZM3Setup.InScene<StartPanel>(scene);
            var menuObject = start.transform.Find("bg_menu");
            var menu = menuObject == null ? new GameObject("bg_menu", typeof(RectTransform), typeof(Image)).GetComponent<Image>() : menuObject.GetComponent<Image>();
            menu.transform.SetParent(start.transform, false);
            menu.transform.SetAsFirstSibling();
            menu.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_BusJam/Art/Textures/bg_menu.png");
            FitArtwork(menu, (RectTransform)ui.GetComponentInParent<Canvas>().transform, new Rect(0, 0, 1, 1));
            var blocker = start.GetComponent<Image>();
            if (blocker != null) blocker.color = Color.clear;

            var art = AssetDatabase.LoadAssetAtPath<UIArt>("Assets/_BusJam/UI/UIArt.asset");
            int plates = 0;
            foreach (var image in ui.GetComponentsInChildren<Image>(true))
            {
                if (image.transform.parent.name != "LivesCounter" && image.transform.parent.name != "Countdown") continue;
                if (image.sprite != art.panel_header && image.sprite != art.counter_plate) continue;
                image.sprite = art.counter_plate;
                image.name = "counter_plate";
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
                image.pixelsPerUnitMultiplier = art.counter_plate.rect.height / image.rectTransform.rect.height
                    * image.canvas.referencePixelsPerUnit / art.counter_plate.pixelsPerUnit;
                plates++;
            }
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fonts + "Fredoka-Bold SDF.asset");
            var settings = new SerializedObject(TMP_Settings.instance);
            settings.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            settings.ApplyModifiedPropertiesWithoutUndo();
            int texts = 0;
            foreach (var text in ui.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
                text.fontStyle &= ~FontStyles.Bold;
                text.UpdateMeshPadding();
                EditorUtility.SetDirty(text);
                texts++;
            }
            var tap = start.transform.Find("TapToStartText").GetComponent<TMP_Text>();
            tap.color = Color.white;
            tap.rectTransform.anchorMin = tap.rectTransform.anchorMax = new Vector2(0.5f, 0.22f);
            tap.rectTransform.anchoredPosition = Vector2.zero;
            tap.rectTransform.sizeDelta = new Vector2(850, 120);
            var time = start.transform.Find("StartTimeText").GetComponent<TMP_Text>();
            time.color = Color.white;
            time.rectTransform.anchorMin = time.rectTransform.anchorMax = new Vector2(0.5f, 0.16f);
            time.rectTransform.anchoredPosition = Vector2.zero;
            EditorUtility.SetDirty(TMP_Settings.instance);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return $"Saved: {plates} counter plates, {texts} Fredoka texts, full-screen menu; production interval unchanged.";
        }

        private static void FitArtwork(Image image, RectTransform canvas, Rect frame)
        {
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = false;
            var artwork = image.GetComponent<CanvasArtwork>() ?? image.gameObject.AddComponent<CanvasArtwork>();
            ZM3Setup.Set(artwork, "canvasRect", canvas);
            ZM3Setup.Set(artwork, "image", image);
            ZM3Setup.Set(artwork, "frame", frame);
            artwork.Fit();
            EditorUtility.SetDirty(artwork);
        }

        public static string Capture(string phase, int width, int height, string panel = "HudPanel", int levelNumber = 4)
        {
            ZM3Setup.RequireEditMode();
            if (cleanup != null) throw new InvalidOperationException("Wait for the previous capture to finish.");
            var scene = SceneManager.GetActiveScene();
            var safe = ZM3Setup.InScene<UIManager>(scene).transform;
            var panels = new[] { "StartPanel", "HudPanel", "CompletePanel", "FailPanel", "NoLivesPanel" };
            bool[] active = panels.Select(n => safe.Find(n).gameObject.activeSelf).ToArray();
            var savedTexts = safe.GetComponentsInChildren<TMP_Text>(true);
            string[] textValues = savedTexts.Select(t => t.text).ToArray();
            var countdowns = safe.GetComponentsInChildren<LivesDisplay>(true).Select(d => ZM3Setup.Get<GameObject>(d, "countdownGroup")).ToArray();
            bool[] countdownActive = countdowns.Select(g => g.activeSelf).ToArray();
            var fixture = new GameObject("ZBG disposable Edit preview") { hideFlags = HideFlags.DontSave };
            BoardView board = null;
            var viewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var view = EditorWindow.GetWindow(viewType);
            bool clear = (bool)viewType.GetField("m_ClearInEditMode", Flags).GetValue(view);
            Texture2D pixels = null;
            var previous = RenderTexture.active;
            cleanup = () =>
            {
                RenderTexture.active = previous;
                if (pixels != null) Object.DestroyImmediate(pixels);
                if (board != null)
                {
                    var mesh = ZM3Setup.Get<Mesh>(board, "baseMesh");
                    ZM3Setup.Set(board, "baseMesh", null);
                    if (mesh != null) Object.DestroyImmediate(mesh);
                }
                Object.DestroyImmediate(fixture);
                for (int i = 0; i < panels.Length; i++) safe.Find(panels[i]).gameObject.SetActive(active[i]);
                for (int i = 0; i < savedTexts.Length; i++) savedTexts[i].text = textValues[i];
                for (int i = 0; i < countdowns.Length; i++) countdowns[i].SetActive(countdownActive[i]);
                viewType.GetField("m_ClearInEditMode", Flags).SetValue(view, clear);
                cleanup = null;
            };
            try
            {
                var level = levelNumber == 0 ? MaximumBoard() : AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/_BusJam/Data/Levels/Level_{levelNumber:00}.asset");
                board = Object.Instantiate(ZM3Setup.InScene<BoardView>(scene), fixture.transform);
                var state = new Board();
                state.Setup(level);
                board.Build(level, state);
                if (levelNumber == 0) Object.DestroyImmediate(level);
                var waiting = Object.Instantiate(ZM3Setup.InScene<WaitingArea>(scene), fixture.transform);
                waiting.Setup(50);
                var station = ZM3Setup.InScene<BusStation>(scene);
                var prefab = ZM3Setup.Get<Bus>(station, "busPrefab");
                var door = ZM3Setup.Get<Transform>(station, "doorPoint");
                var materials = ZM3Setup.Get<Material[]>(station, "colorMaterials");
                for (int i = 0; i < 3; i++)
                {
                    var bus = Object.Instantiate(prefab, fixture.transform);
                    bus.transform.position = door.position + Vector3.left * i * ZM3Setup.Get<float>(station, "queueSpacing");
                    var body = ZM3Setup.Get<Renderer>(bus, "body");
                    var slots = body.sharedMaterials;
                    slots[0] = materials[levelNumber == 0 ? i % 2 : (int)level.busOrder[i % level.busOrder.Count]];
                    body.sharedMaterials = slots;
                }
                foreach (var animator in fixture.GetComponentsInChildren<Animator>())
                {
                    var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name.Contains("Idle"));
                    if (clip != null) clip.SampleAnimation(animator.gameObject, 0);
                }
                for (int i = 0; i < panels.Length; i++) safe.Find(panels[i]).gameObject.SetActive(panels[i] == panel);
                foreach (var display in safe.GetComponentsInChildren<LivesDisplay>(true))
                {
                    ZM3Setup.Set(display, "lastCount", -1);
                    ZM3Setup.Set(display, "lastSeconds", -1);
                    typeof(LivesDisplay).GetMethod("Show", Flags).Invoke(display, new object[] { panel == "NoLivesPanel" ? 0 : 3, 754 });
                }
                safe.Find("HudPanel/LevelText").GetComponent<TMP_Text>().text = levelNumber == 0 ? "Max 7x9" : "Level " + levelNumber;
                viewType.GetMethod("SetCustomResolution", Flags).Invoke(view, new object[] { new Vector2(width, height), "ZBG " + width + "x" + height });
                viewType.GetField("m_ClearInEditMode", Flags).SetValue(view, false);
                viewType.GetProperty("showGizmos", Flags).SetValue(view, false, null);
                view.Focus();
                view.Repaint();
                Canvas.ForceUpdateCanvases();
                string path = $"Docs/ZBG-Captures/{phase}-{panel}-L{levelNumber:00}-{width}x{height}.png";
                captureResult = "Pending " + path;
                EditorApplication.delayCall += () =>
                {
                    view.Repaint();
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            Canvas.ForceUpdateCanvases();
                            foreach (var graphic in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Graphic>()))
                            {
                                graphic.SetAllDirty();
                                graphic.Rebuild(CanvasUpdate.PreRender);
                            }
                            var texture = (RenderTexture)viewType.GetMethod("RenderView", Flags).Invoke(view, new object[] { Vector2.zero, false });
                            if (texture.width != width || texture.height != height)
                                throw new InvalidOperationException($"Game view rendered {texture.width}x{texture.height}, expected {width}x{height}.");
                            RenderTexture.active = texture;
                            pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                            var raw = pixels.GetPixels32();
                            if (SystemInfo.graphicsUVStartsAtTop)
                                for (int y = 0; y < height / 2; y++)
                                    for (int x = 0; x < width; x++)
                                    {
                                        int a = y * width + x, b = (height - 1 - y) * width + x;
                                        var temp = raw[a]; raw[a] = raw[b]; raw[b] = temp;
                                    }
                            pixels.SetPixels32(raw);
                            pixels.Apply();
                            Directory.CreateDirectory("Docs/ZBG-Captures");
                            File.WriteAllBytes(path, pixels.EncodeToPNG());
                            captureResult = path;
                        }
                        catch (Exception exception) { captureResult = exception.ToString(); Debug.LogException(exception); }
                        finally { cleanup?.Invoke(); }
                    };
                };
                return captureResult;
            }
            catch
            {
                cleanup?.Invoke();
                throw;
            }
        }

        private static LevelData MaximumBoard()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.Resize(7, 9);
            return level;
        }

        public static string ValidateGeometry(int width, int height)
        {
            ZM3Setup.RequireEditMode();
            var scene = SceneManager.GetActiveScene();
            var camera = ZM3Setup.InScene<Camera>(scene);
            float aspect = camera.aspect;
            var output = new System.Text.StringBuilder();
            var fixture = new GameObject("ZBG geometry fixture") { hideFlags = HideFlags.DontSave };
            BoardView board = null;
            try
            {
                camera.aspect = (float)width / height;
                for (int number = 0; number <= 10; number++)
                {
                    var level = number == 0 ? MaximumBoard() : AssetDatabase.LoadAssetAtPath<LevelData>($"Assets/_BusJam/Data/Levels/Level_{number:00}.asset");
                    board = Object.Instantiate(ZM3Setup.InScene<BoardView>(scene), fixture.transform);
                    var state = new Board(); state.Setup(level); board.Build(level, state);
                    Vector2 min = Vector2.one, max = Vector2.zero;
                    foreach (var renderer in board.GetComponentsInChildren<Renderer>())
                    {
                        if (!renderer.enabled || renderer.GetComponentInParent<Passenger>() != null) continue;
                        var bounds = renderer.bounds;
                        for (int corner = 0; corner < 8; corner++)
                        {
                            Vector3 world = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                            Vector2 point = camera.WorldToViewportPoint(world);
                            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                        }
                    }
                    bool inside = min.x >= 0 && max.x <= 1 && min.y >= 0 && max.y <= 1 - 559f / 1672 * 1.3f;
                    output.AppendLine($"{width}x{height} / {(number == 0 ? "max 7x9" : "Level_" + number.ToString("00"))}: {(inside ? "PASS" : "FAIL")}, x {min.x * 100:F3}..{max.x * 100:F3}%, top {(1 - max.y) * 100:F3}%, bottom {(1 - min.y) * 100:F3}%");
                    if (number == 0) Object.DestroyImmediate(level);
                    DisposeBoard(board); board = null;
                }
                var station = ZM3Setup.InScene<BusStation>(scene);
                var bus = Object.Instantiate(ZM3Setup.Get<Bus>(station, "busPrefab"), fixture.transform);
                bus.transform.position = ZM3Setup.Get<Transform>(station, "doorPoint").position;
                var boundsBody = ZM3Setup.Get<Renderer>(bus, "body").bounds;
                float top = 1, bottom = 0;
                for (int i = 0; i < 8; i++)
                {
                    var point = boundsBody.center + Vector3.Scale(boundsBody.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    float y = 1 - camera.WorldToViewportPoint(point).y;
                    top = Mathf.Min(top, y); bottom = Mathf.Max(bottom, y);
                }
                bool busInside = top >= 216f / 1672 * 1.3f && bottom <= 364f / 1672 * 1.3f;
                output.AppendLine($"Bus body on asphalt: {(busInside ? "PASS" : "FAIL")}, top {top * 100:F3}%, bottom {bottom * 100:F3}%");
                float waiting = 1 - camera.WorldToViewportPoint(ZM3Setup.InScene<WaitingArea>(scene).transform.position).y;
                output.AppendLine($"Slot ground center: {waiting * 100:F3}% from top; sidewalk midpoint {(404f + 534) * 0.5f / 1672 * 130:F3}%.");
                return output.ToString();
            }
            finally
            {
                camera.aspect = aspect;
                if (board != null) DisposeBoard(board);
                Object.DestroyImmediate(fixture);
            }
        }

        private static void DisposeBoard(BoardView board)
        {
            var mesh = ZM3Setup.Get<Mesh>(board, "baseMesh");
            ZM3Setup.Set(board, "baseMesh", null);
            if (mesh != null) Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(board.gameObject);
        }
    }
}
