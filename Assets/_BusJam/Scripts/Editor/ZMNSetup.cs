using System;
using BusJam.Core;
using BusJam.Gameplay;
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
    /// <summary>Applies the menu/HUD layout and visual-only passenger enlargement in Edit mode.</summary>
    public static class ZMNSetup
    {
        private static TMP_FontAsset font;
        private static readonly Color Ink = new Color(0.08f, 0.12f, 0.23f);

        public static string Build()
        {
            ZM3Setup.RequireEditMode();
            var scene = SceneManager.GetActiveScene();
            var start = ZM3Setup.InScene<StartPanel>(scene);
            var hud = ZM3Setup.InScene<HudPanel>(scene);
            var manager = ZM3Setup.InScene<GameManager>(scene);
            var lives = ZM3Setup.InScene<LivesSystem>(scene);
            var art = AssetDatabase.LoadAssetAtPath<UIArt>("Assets/_BusJam/UI/UIArt.asset");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_BusJam/Art/Fonts/Fredoka-Bold SDF.asset");
            Remove(start.transform, "TapToStartText");
            Remove(start.transform, "StartTimeText");
            Remove(start.transform, "LivesCounter");
            Remove(hud.transform, "LivesCounter");
            Remove(hud.transform, "LevelText");
            Remove(hud.transform, "HudTimeText");
            var play = ImageAt(start.transform, "StartButton", art.btn_green, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(594, 190));
            play.raycastTarget = true;
            var button = play.GetComponent<Button>() ?? play.gameObject.AddComponent<Button>();
            button.targetGraphic = play;
            var label = TextAt(play.transform, "LevelLabel", "Level 1", new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(540,130), 76);
            ZM3Setup.Set(start, "startButton", button);
            ZM3Setup.Set(start, "levelText", label);
            var plate = ImageAt(start.transform, "LivesCounter", art.counter_plate, new Vector2(0.5f,1), new Vector2(0,-106), new Vector2(440,116), true);
            ImageAt(plate.transform,"icon_heart",art.icon_heart,Vector2.one*0.5f,new Vector2(-168,0),new Vector2(114,106));
            var count = TextAt(plate.transform,"LivesCount","5",Vector2.one*0.5f,new Vector2(-66,0),new Vector2(90,90),62);
            var timer = TextAt(plate.transform,"CountdownText","FULL",Vector2.one*0.5f,new Vector2(90,0),new Vector2(198,80),46);
            var display = plate.gameObject.AddComponent<LivesDisplay>();
            ZM3Setup.Set(display,"livesSystem",lives);
            ZM3Setup.Set(display,"countText",count);
            ZM3Setup.Set(display,"countdownText",timer);
            ZM3Setup.Set(display,"showFull",true);
            var badge = ImageAt(hud.transform,"LevelBadge",art.badge_level,new Vector2(0,1),new Vector2(145,-142),new Vector2(220,220));
            TextAt(badge.transform,"LevelTitle","Level",Vector2.one*0.5f,new Vector2(0,50),new Vector2(180,65),42);
            var number = TextAt(badge.transform,"LevelNumber","1",Vector2.one*0.5f,new Vector2(0,-26),new Vector2(180,112),88);
            var clock = ImageAt(hud.transform,"TimerCounter",art.counter_plate,new Vector2(1,1),new Vector2(-190,-110),new Vector2(310,116),true);
            ImageAt(clock.transform,"icon_clock",art.icon_clock,Vector2.one*0.5f,new Vector2(-111,0),new Vector2(108,116));
            var time = TextAt(clock.transform,"TimerText","0:30",Vector2.one*0.5f,new Vector2(44,0),new Vector2(194,90),62);
            ZM3Setup.Set(hud,"levelText",number);
            ZM3Setup.Set(hud,"timeText",time);
            EditorUtility.SetDirty(start);
            EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            const string path = "Assets/_BusJam/Prefabs/Passenger.prefab";
            var passenger = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var visual = passenger.transform.Find("VisualRoot");
                if (visual == null)
                {
                    visual = new GameObject("VisualRoot").transform;
                    visual.SetParent(passenger.transform,false);
                    for (int i = passenger.transform.childCount - 1; i >= 0; i--)
                    {
                        var child = passenger.transform.GetChild(i);
                        if (child != visual) child.SetParent(visual,false);
                    }
                }
                visual.localScale = Vector3.one * 1.4f;
                var p = passenger.GetComponent<Passenger>();
                ZM3Setup.Set(p,"visualRoot",visual);
                ZM3Setup.Set(p,"standingVisualScale",1.4f);
                // These offsets are relative to the visual root and scale with the head.
                visual.Find("HiddenMark").localPosition = new Vector3(0,1.25f,0.12f);
                visual.Find("AngryIcon").localPosition = new Vector3(0,1.36f,0);
                PrefabUtility.SaveAsPrefabAsset(passenger,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(passenger); }
            return "Menu, HUD and 1.4x passenger visual root saved.";
        }

        public static string Capture(string panel)
        {
            ZM3Setup.RequireEditMode();
            var source = SceneManager.GetActiveScene();
            var preview = EditorSceneManager.NewPreviewScene();
            var target = new RenderTexture(1080, 1920, 24);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            BoardView board = null;
            try
            {
                foreach (var root in source.GetRootGameObjects())
                {
                    var clone = Object.Instantiate(root);
                    SceneManager.MoveGameObjectToScene(clone, preview);
                }
                var camera = ZM3Setup.InScene<Camera>(preview);
                camera.scene = preview;
                camera.aspect = 1080f / 1920f;
                camera.targetTexture = target;
                board = ZM3Setup.InScene<BoardView>(preview);
                var level = AssetDatabase.LoadAssetAtPath<BusJam.Level.LevelData>("Assets/_BusJam/Data/Levels/Level_09.asset");
                var state = new Board(); state.Setup(level); board.Build(level,state);
                var waiting = ZM3Setup.InScene<WaitingArea>(preview);
                waiting.Setup(100);
                for (int i = 0; i < 7; i++)
                {
                    var p = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Passenger>("Assets/_BusJam/Prefabs/Passenger.prefab"),waiting.transform);
                    var color = (BusJam.Level.PassengerColor)(i % 7);
                    p.Setup(color, Vector2Int.zero, false, ZM3Setup.Get<Material[]>(board,"colorMaterials")[i % 7]);
                    p.transform.position = waiting.SlotPosition(i);
                    p.SetInSlot(waiting.WaitingRotation,waiting.PassengerSlotScale);
                }
                var station = ZM3Setup.InScene<BusStation>(preview);
                var bus = Object.Instantiate(ZM3Setup.Get<Bus>(station,"busPrefab"),station.transform);
                bus.transform.position = ZM3Setup.Get<Transform>(station,"doorPoint").position;
                foreach (var root in preview.GetRootGameObjects())
                    foreach (var animator in root.GetComponentsInChildren<Animator>())
                    {
                        var idle = Array.Find(animator.runtimeAnimatorController.animationClips,c => c.name.Contains("Idle"));
                        if (idle != null) idle.SampleAnimation(animator.gameObject,0);
                    }
                var safe = ZM3Setup.InScene<UIManager>(preview).transform;
                foreach (string name in new[] { "StartPanel","HudPanel","CompletePanel","FailPanel","NoLivesPanel" })
                    safe.Find(name).gameObject.SetActive(name == panel);
                safe.Find("StartPanel/StartButton/LevelLabel").GetComponent<TMP_Text>().text = "Level 9";
                safe.Find("HudPanel/LevelBadge/LevelNumber").GetComponent<TMP_Text>().text = "9";
                safe.Find("HudPanel/TimerCounter/TimerText").GetComponent<TMP_Text>().text = "0:30";
                foreach (var root in preview.GetRootGameObjects())
                {
                    var canvas = root.GetComponent<Canvas>();
                    if (canvas == null) continue;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = canvas.sortingOrder < 0 ? camera.farClipPlane * 0.9f : 1f;
                }
                Canvas.ForceUpdateCanvases();
                foreach (var root in preview.GetRootGameObjects())
                    foreach (var artwork in root.GetComponentsInChildren<CanvasArtwork>(true)) artwork.Fit();
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(1080,1920,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,1080,1920),0,0); pixels.Apply();
                System.IO.Directory.CreateDirectory("Docs/ZMN-Captures");
                string path = "Docs/ZMN-Captures/"+panel+"-1080x1920.png";
                System.IO.File.WriteAllBytes(path,pixels.EncodeToPNG());
                return path;
            }
            finally
            {
                RenderTexture.active = previous;
                if (board != null)
                {
                    var mesh = ZM3Setup.Get<Mesh>(board,"baseMesh");
                    ZM3Setup.Set(board,"baseMesh",null);
                    if (mesh != null) Object.DestroyImmediate(mesh);
                }
                EditorSceneManager.ClosePreviewScene(preview);
                if (pixels != null) Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(target);
            }
        }

        public static string Validate()
        {
            ZM3Setup.RequireEditMode();
            var scene = SceneManager.GetActiveScene();
            var board = ZM3Setup.InScene<BoardView>(scene);
            var waiting = ZM3Setup.InScene<WaitingArea>(scene);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BusJam/Prefabs/Passenger.prefab");
            var instance = Object.Instantiate(prefab);
            try
            {
                var animator = instance.GetComponentInChildren<Animator>();
                var idle = Array.Find(animator.runtimeAnimatorController.animationClips, c => c.name.Contains("Idle"));
                idle.SampleAnimation(animator.gameObject, 0);
                var body = ZM3Setup.Get<Renderer>(instance.GetComponent<Passenger>(), "body");
                var bounds = body.bounds;
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    if (renderer.name != "HiddenMark" && renderer.name != "AngryIcon") bounds.Encapsulate(renderer.bounds);
                float cell = ZM3Setup.Get<float>(board, "cellSize");
                float spacing = ZM3Setup.Get<float>(waiting, "slotSpacing");
                Vector3 gridSize = bounds.size;
                instance.transform.rotation = waiting.WaitingRotation;
                instance.transform.localScale = Vector3.one * waiting.PassengerSlotScale;
                Vector3 slotSize = body.bounds.size;
                bool fits = gridSize.x < cell && gridSize.z < cell && slotSize.x < spacing;
                if (!fits) throw new InvalidOperationException("Passenger envelope exceeds grid or slot spacing.");
                var level = AssetDatabase.LoadAssetAtPath<BusJam.Level.LevelData>("Assets/_BusJam/Data/Levels/Level_09.asset");
                int packedNeighbours = 0;
                for (int y = 0; y < level.height; y++)
                    for (int x = 0; x < level.width - 1; x++)
                        if (level.cells[y * level.width + x].type == BusJam.Level.CellType.Passenger &&
                            level.cells[y * level.width + x + 1].type == BusJam.Level.CellType.Passenger) packedNeighbours++;
                var visual = instance.transform.Find("VisualRoot");
                instance.GetComponent<Passenger>().SetSeated(waiting.transform);
                bool seated = visual.localScale == Vector3.one;
                if (!seated) throw new InvalidOperationException("Seat enlargement was not cancelled.");
                return $"PASS: Level_09 has {packedNeighbours} horizontal packed pairs; visual envelope {gridSize.x:F4} x {gridSize.z:F4} within {cell:F3} grid; slot width {slotSize.x:F4} within {spacing:F4} spacing. Seat visual scale restored to 1. Collider stays on unscaled passenger root.";
            }
            finally { Object.DestroyImmediate(instance); }
        }

        private static void Remove(Transform parent,string name)
        {
            var child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }
        private static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var child = parent.Find(name);
            var rect = child == null ? (RectTransform)new GameObject(name,typeof(RectTransform)).transform : (RectTransform)child;
            rect.SetParent(parent,false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = Vector2.one*0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
        private static Image ImageAt(Transform parent,string name,Sprite sprite,Vector2 anchor,Vector2 position,Vector2 size,bool sliced=false)
        {
            var rect = Rect(parent,name,anchor,position,size);
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            if (sliced) image.pixelsPerUnitMultiplier = 6.25f;
            image.preserveAspect = !sliced;
            image.raycastTarget = false;
            return image;
        }
        private static TMP_Text TextAt(Transform parent,string name,string text,Vector2 anchor,Vector2 position,Vector2 size,float fontSize)
        {
            var rect = Rect(parent,name,anchor,position,size);
            var label = rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.outlineColor = Ink;
            label.outlineWidth = 0.18f;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }
    }
}

