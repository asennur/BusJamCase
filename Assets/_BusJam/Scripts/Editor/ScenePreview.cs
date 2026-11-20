using System;
using BusJam.Gameplay;
using BusJam.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Builds a saved, editor-only level preview without loading progress or starting gameplay.</summary>
    [InitializeOnLoad]
    public static class ScenePreview
    {
        private const string RootName = "_ScenePreview";
        private const string GameScene = "Assets/_BusJam/Scenes/Game.unity";

        static ScenePreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) Clear();
            };
        }

        [MenuItem("Bus Jam/Scene Preview/Show Level 4")]
        public static void ShowLevel4() => ShowLevel(4);

        public static void ShowLevel(int levelNumber)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (levelNumber < 1) throw new ArgumentOutOfRangeException(nameof(levelNumber));
            var level = AssetDatabase.LoadAssetAtPath<LevelData>(
                $"Assets/_BusJam/Data/Levels/Level_{levelNumber:00}.asset");
            if (level == null) throw new ArgumentException($"Level {levelNumber} was not found.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != GameScene) scene = EditorSceneManager.OpenScene(GameScene);
            Clear();
            var sourceBoard = Find<BoardView>(scene);
            var sourceWaiting = Find<WaitingArea>(scene);
            var station = new SerializedObject(Find<BusStation>(scene));
            var root = new GameObject(RootName) { tag = "EditorOnly" };
            SceneManager.MoveGameObjectToScene(root, scene);

            var boardView = CopySettings(sourceBoard, root.transform);
            var board = new Board();
            board.Setup(level);
            boardView.Build(level, board);
            var reachable = new bool[board.CellCount];
            board.ComputeReachable(reachable);
            for (int i = 0; i < board.CellCount; i++)
            {
                var passenger = board.GetOccupant(i);
                if (passenger != null) passenger.SetReachable(reachable[i]);
            }
            LinkPrefabs(boardView, sourceBoard);
            var waiting = CopySettings(sourceWaiting, root.transform);
            waiting.Setup(levelNumber);
            LinkPrefabs(waiting, sourceWaiting);

            // Bus.Setup requires play-mode Awake/tweens; use its production prefab and shared body materials.
            var busPrefab = (Bus)station.FindProperty("busPrefab").objectReferenceValue;
            var door = (Transform)station.FindProperty("doorPoint").objectReferenceValue;
            var materials = station.FindProperty("colorMaterials");
            float spacing = station.FindProperty("queueSpacing").floatValue;
            int count = Mathf.Min(station.FindProperty("visibleQueueCount").intValue, level.busOrder.Count);
            for (int i = 0; i < count; i++)
            {
                var bus = (Bus)PrefabUtility.InstantiatePrefab(busPrefab, root.transform);
                bus.name = $"Bus {i + 1} ({level.busOrder[i]})";
                bus.transform.position = door.position - door.right * (spacing * i);
                var settings = new SerializedObject(bus);
                var body = (Renderer)settings.FindProperty("body").objectReferenceValue;
                var shared = body.sharedMaterials;
                shared[0] = (Material)materials.GetArrayElementAtIndex((int)level.busOrder[i]).objectReferenceValue;
                body.sharedMaterials = shared;
                var indicators = settings.FindProperty("seatIndicators");
                for (int seat = 0; seat < indicators.arraySize; seat++)
                    ((GameObject)indicators.GetArrayElementAtIndex(seat).objectReferenceValue)?.SetActive(false);
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            Find<CameraFitter>(scene).SetLevel(level);
            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
        }

        [MenuItem("Bus Jam/Scene Preview/Clear")]
        public static void Clear()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.name == RootName && root.CompareTag("EditorOnly"))
                        Object.DestroyImmediate(root);
            }
        }

        private static T CopySettings<T>(T source, Transform parent) where T : Component
        {
            var copy = new GameObject(source.name);
            copy.transform.SetParent(parent, false);
            copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            copy.transform.localScale = source.transform.lossyScale;
            var component = copy.AddComponent<T>();
            EditorUtility.CopySerialized(source, component);
            return component;
        }

        // Retain production prefab links so the saved preview stays compact and follows art changes.
        private static void LinkPrefabs(Component preview, Component source)
        {
            var property = new SerializedObject(source).GetIterator();
            var settings = new ConvertToPrefabInstanceSettings { recordPropertyOverridesOfMatches = true };
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                var prefab = property.objectReferenceValue as Component;
                if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab)) continue;
                foreach (Transform child in preview.transform)
                    if (child.name == prefab.name + "(Clone)")
                        PrefabUtility.ConvertToPrefabInstance(child.gameObject, prefab.gameObject,
                            settings, InteractionMode.AutomatedAction);
            }
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == RootName) continue;
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            throw new InvalidOperationException($"{typeof(T).Name} is missing from {scene.name}.");
        }
    }
}
