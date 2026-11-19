using System;
using System.Linq;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Save;
using BusJam.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Rebinds the replacement model and counters without changing the authored UI layout.</summary>
    public static class ZPAX2Setup
    {
        private const string PassengerPath = "Assets/_BusJam/Prefabs/Passenger.prefab";
        private const string ModelPath = "Assets/_BusJam/Art/Models/Yolcu/Yolcu.fbx";
        private const string BusModelPath = "Assets/_BusJam/Art/Models/Otobus/Otobus.fbx";

        private static AnimationClip Clip(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => c.name == name);

        public static string Build()
        {
            ZM3Setup.RequireEditMode();
            var scene = SceneManager.GetActiveScene();
            var layout = ZPAX2Verification.Layout(scene);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var display in root.GetComponentsInChildren<LivesDisplay>(true))
            {
                if (display.name.IndexOf("Money", StringComparison.OrdinalIgnoreCase) < 0 &&
                    display.name.IndexOf("Coin", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var text = ZM3Setup.Get<TMP_Text>(display, "countText") ?? ZM3Setup.Get<TMP_Text>(display, "countdownText");
                var counter = display.GetComponent<CoinCounter>() ?? display.gameObject.AddComponent<CoinCounter>();
                ZM3Setup.Set(counter, "countText", text);
                Object.DestroyImmediate(display);
                counter.Show(SaveSystem.Coins);
                EditorUtility.SetDirty(counter);
            }
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var counter in root.GetComponentsInChildren<CoinCounter>(true)) counter.Show(SaveSystem.Coins);
                foreach (var display in root.GetComponentsInChildren<LivesDisplay>(true))
                {
                    var count = ZM3Setup.Get<TMP_Text>(display, "countText");
                    var time = ZM3Setup.Get<TMP_Text>(display, "countdownText");
                    if (count != null) count.text = display.name == "NoLivesPanel" ? "0" : "5";
                    if (time != null) time.text = display.name == "NoLivesPanel" ? "Next life in 20:00" : "FULL";
                }
            }
            if (layout != ZPAX2Verification.Layout(scene)) throw new InvalidOperationException("Authored UI layout changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.motionNodeName = "Root";
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.clipAnimations = importer.defaultClipAnimations.Where(c => c.name != "SlotIdle").Select(c =>
            {
                c.loopTime = c.name != "Sit";
                return c;
            }).ToArray();
            importer.SaveAndReimport();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_BusJam/Art/Models/Yolcu/Yolcu_Animator.controller");
            foreach (var state in controller.layers[0].stateMachine.states) state.state.motion = Clip(ModelPath, state.state.name);
            EditorUtility.SetDirty(controller);
            var passenger = PrefabUtility.LoadPrefabContents(PassengerPath);
            try
            {
                var p = passenger.GetComponent<Passenger>();
                var visual = ZM3Setup.Get<Transform>(p, "visualRoot");
                var old = visual.Find("Yolcu");
                var position = old.localPosition;
                var rotation = old.localRotation;
                var scale = old.localScale;
                Object.DestroyImmediate(old.gameObject);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), visual);
                model.name = "Yolcu";
                model.transform.localPosition = position;
                model.transform.localRotation = rotation;
                model.transform.localScale = scale;
                var animator = model.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                var body = model.GetComponentInChildren<SkinnedMeshRenderer>();
                body.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BusJam/Materials/M_Red.mat");
                var hull = new GameObject("Outline", typeof(SkinnedMeshRenderer));
                hull.transform.SetParent(body.transform, false);
                var outline = hull.GetComponent<SkinnedMeshRenderer>();
                outline.sharedMesh = body.sharedMesh;
                outline.bones = body.bones;
                outline.rootBone = body.rootBone;
                outline.localBounds = body.localBounds;
                outline.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BusJam/Materials/M_OutlineHull.mat");
                outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                hull.SetActive(false);
                ZM3Setup.Set(p, "body", body);
                ZM3Setup.Set(p, "animator", animator);
                ZM3Setup.Set(p, "outline", hull);
                visual.localScale = Vector3.one * 1.4f;
                for (int i = 0; i < 4; i++)
                {
                    var rope = visual.Find("Rope_" + (i + 1));
                    rope.localPosition = new Vector3(0, .40f + .10f * i, 0);
                    rope.localScale = new Vector3(.50f, .026f, .43f);
                }
                visual.Find("Cloak").localPosition = new Vector3(0, .66f, 0);
                visual.Find("Cloak/Mantle").localScale = new Vector3(.51f, .32f, .38f);
                visual.Find("Cloak/GoldHem").localPosition = new Vector3(0, -.45f, 0);
                visual.Find("Cloak/GoldHem").localScale = new Vector3(.515f, .012f, .385f);
                visual.Find("HiddenMark").localPosition = new Vector3(0, 1.25f, .12f);
                visual.Find("AngryIcon").localPosition = new Vector3(0, 1.40f, 0);
                PrefabUtility.SaveAsPrefabAsset(passenger, PassengerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(passenger); }
            BuildBus();
            AssetDatabase.SaveAssets();
            return "Replacement passenger, full seated scale, coin bindings and looping bus exhaust saved. All UI transforms/hierarchy preserved.";
        }

        private static void BuildBus()
        {
            const string smokePath = "Assets/_BusJam/Art/Models/Otobus/ExhaustLoop.anim";
            var smoke = AssetDatabase.LoadAssetAtPath<AnimationClip>(smokePath);
            if (smoke == null) { smoke = new AnimationClip { name = "ExhaustLoop", frameRate = 30 }; AssetDatabase.CreateAsset(smoke, smokePath); }
            var idle = Clip(BusModelPath, "Idle");
            foreach (var binding in AnimationUtility.GetCurveBindings(idle))
                if (binding.path.Contains("Smoke_")) AnimationUtility.SetEditorCurve(smoke, binding, AnimationUtility.GetEditorCurve(idle, binding));
            var settings = AnimationUtility.GetAnimationClipSettings(smoke);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(smoke, settings);
            EditorUtility.SetDirty(smoke);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_BusJam/Art/Models/Otobus/Otobus_Animator.controller");
            var layers = controller.layers;
            int index = Array.FindIndex(layers, l => l.name == "Exhaust");
            if (index < 0) { controller.AddLayer("Exhaust"); layers = controller.layers; index = layers.Length - 1; }
            layers[index].defaultWeight = 1;
            var machine = layers[index].stateMachine;
            var state = machine.states.Length == 0 ? machine.AddState("ExhaustLoop") : machine.states[0].state;
            state.motion = smoke;
            state.writeDefaultValues = false;
            machine.defaultState = state;
            var mask = layers[index].avatarMask;
            if (mask == null)
            {
                mask = new AvatarMask { name = "ExhaustOnly" };
                AssetDatabase.AddObjectToAsset(mask, controller);
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BusModelPath);
            var transforms = model.GetComponentsInChildren<Transform>(true);
            mask.transformCount = transforms.Length;
            for (int i = 0; i < transforms.Length; i++)
            {
                mask.SetTransformPath(i, AnimationUtility.CalculateTransformPath(transforms[i], model.transform));
                mask.SetTransformActive(i, transforms[i].name.StartsWith("Smoke_", StringComparison.Ordinal));
            }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            layers[index].avatarMask = mask;
            EditorUtility.SetDirty(mask);
            controller.layers = layers;
            EditorUtility.SetDirty(controller);

            const string materialPath = "Assets/_BusJam/Materials/M_ExhaustSmoke.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_BusJam/Materials/M_BusBody.mat"));
                material.name = "M_ExhaustSmoke";
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetColor("_BaseColor", new Color(.7f, .73f, .76f, 1));
            EditorUtility.SetDirty(material);
            var importer = (ModelImporter)AssetImporter.GetAtPath(BusModelPath);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_BusSmoke"), material);
            importer.SaveAndReimport();
            const string path = "Assets/_BusJam/Prefabs/Bus.prefab";
            var bus = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var b = bus.GetComponent<Bus>();
                ZM3Setup.Set(b, "model", bus.transform.Find("Otobus"));
                var body = ZM3Setup.Get<Renderer>(b, "body");
                var materials = body.sharedMaterials;
                materials[materials.Length - 1] = material;
                body.sharedMaterials = materials;
                PrefabUtility.SaveAsPrefabAsset(bus, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(bus); }
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
                foreach (var root in source.GetRootGameObjects()) SceneManager.MoveGameObjectToScene(Object.Instantiate(root), preview);
                var camera = ZM3Setup.InScene<Camera>(preview);
                camera.scene = preview;
                camera.aspect = 1080f / 1920f;
                camera.targetTexture = target;
                board = ZM3Setup.InScene<BoardView>(preview);
                var level = AssetDatabase.LoadAssetAtPath<BusJam.Level.LevelData>("Assets/_BusJam/Data/Levels/Level_09.asset");
                var state = new Board(); state.Setup(level); board.Build(level, state);
                var waiting = ZM3Setup.InScene<WaitingArea>(preview); waiting.Setup(100);
                var materials = ZM3Setup.Get<Material[]>(board, "colorMaterials");
                var passengers = board.GetComponentsInChildren<Passenger>();
                for (int i = 0; i < Math.Min(4, passengers.Length); i++)
                {
                    var p = passengers[i];
                    p.Setup((BusJam.Level.PassengerColor)i, p.Cell, i == 3,
                        i == 3 ? AssetDatabase.LoadAssetAtPath<Material>("Assets/_BusJam/Materials/M_Hidden.mat") : materials[i],
                        i == 2 ? BusJam.Level.Cloak.StartsClosed : BusJam.Level.Cloak.None);
                    if (i == 0) p.SetReachable(true);
                    if (i == 1) p.SetRopes(4);
                }
                var station = ZM3Setup.InScene<BusStation>(preview);
                var bus = Object.Instantiate(ZM3Setup.Get<Bus>(station, "busPrefab"), station.transform);
                bus.transform.position = ZM3Setup.Get<Transform>(station, "doorPoint").position;
                foreach (var root in preview.GetRootGameObjects())
                foreach (var animator in root.GetComponentsInChildren<Animator>())
                {
                    var idle = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == "Idle");
                    if (idle != null) idle.SampleAnimation(animator.gameObject, .25f);
                }
                var busAnimator = ZM3Setup.Get<Animator>(bus, "animator");
                Clip(BusModelPath, "DoorOpen").SampleAnimation(busAnimator.gameObject, Clip(BusModelPath, "DoorOpen").length);
                AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_BusJam/Art/Models/Otobus/ExhaustLoop.anim").SampleAnimation(busAnimator.gameObject, .35f);
                var seated = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Passenger>(PassengerPath), bus.transform);
                seated.Setup(BusJam.Level.PassengerColor.Red, Vector2Int.zero, false, materials[0]);
                seated.SetSeated(ZM3Setup.Get<Transform[]>(bus, "seatPoints")[1]);
                var seatedAnimator = ZM3Setup.Get<Animator>(seated, "animator");
                Clip(ModelPath, "Sit").SampleAnimation(seatedAnimator.gameObject, Clip(ModelPath, "Sit").length);
                var safe = ZM3Setup.InScene<UIManager>(preview).transform;
                foreach (string name in new[] { "StartPanel", "HudPanel", "CompletePanel", "FailPanel", "NoLivesPanel" }) safe.Find(name).gameObject.SetActive(name == panel);
                foreach (var root in preview.GetRootGameObjects())
                {
                    var canvas = root.GetComponent<Canvas>();
                    if (canvas == null) continue;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                    canvas.planeDistance = canvas.sortingOrder < 0 ? camera.farClipPlane * .9f : 1f;
                }
                Canvas.ForceUpdateCanvases();
                foreach (var root in preview.GetRootGameObjects())
                    foreach (var art in root.GetComponentsInChildren<CanvasArtwork>(true)) art.Fit();
                Canvas.ForceUpdateCanvases(); camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); pixels.Apply();
                System.IO.Directory.CreateDirectory("Docs/ZPAX2-Captures");
                string path = "Docs/ZPAX2-Captures/" + panel + ".png";
                System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG());
                return path;
            }
            finally
            {
                RenderTexture.active = previous;
                if (board != null)
                {
                    var mesh = ZM3Setup.Get<Mesh>(board, "baseMesh"); ZM3Setup.Set(board, "baseMesh", null);
                    if (mesh != null) Object.DestroyImmediate(mesh);
                }
                EditorSceneManager.ClosePreviewScene(preview);
                if (pixels != null) Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(target);
            }
        }
    }
}
