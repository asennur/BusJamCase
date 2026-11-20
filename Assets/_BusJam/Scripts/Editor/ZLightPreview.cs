using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BusJam.Gameplay;
using BusJam.Level;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BusJam.EditorTools
{
    /// <summary>Disposable, deterministic Edit-mode lighting captures. Never starts gameplay or saves previews.</summary>
    public static class ZLightPreview
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        private static GameObject preview;
        private static BoardView boardView;
        private static Bus activeBus;
        private static EditorWindow gameView;
        private static string phase;

        private static T Field<T>(object target, string field) => (T)target.GetType().GetField(field, Flags).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
        private static T SceneObject<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectOfType<T>(true);

        public static string Prepare(string capturePhase, string screen)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required.");
            if (preview != null) Cleanup();
            phase = capturePhase;
            ConfigureGameView();
            preview = new GameObject("ZLIGHT disposable Edit preview") { hideFlags = HideFlags.DontSave };

            foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
            {
                if (canvas.name != "Canvas") continue;
                foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                    if (t.name.EndsWith("Panel", StringComparison.Ordinal))
                        t.gameObject.SetActive(t.name == (screen == "Start" ? "StartPanel" : "HudPanel") || (screen == "Complete" && t.name == "CompletePanel"));
            }
            if (screen != "Start")
            {
                var source = SceneObject<BoardView>();
                boardView = UnityEngine.Object.Instantiate(source, preview.transform);
                boardView.name = "Level 07 board fixture";
                var template = UnityEngine.Object.Instantiate(Field<Passenger>(source, "passengerPrefab"), preview.transform);
                template.name = "Inactive passenger template";
                Freeze(template.gameObject);
                Set(template, "animator", null);
                template.gameObject.SetActive(false);
                Set(boardView, "passengerPrefab", template);
                var level = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/_BusJam/Data/Levels/Level_07.asset");
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
                var waiting = UnityEngine.Object.Instantiate(SceneObject<WaitingArea>(), preview.transform);
                waiting.name = "Seven production slots";
                waiting.Setup(7);
                var station = SceneObject<BusStation>();
                var busPrefab = Field<Bus>(station, "busPrefab");
                var materials = Field<Material[]>(station, "colorMaterials");
                var door = Field<Transform>(station, "doorPoint").position;
                for (int i = 0; i < 3; i++)
                {
                    var bus = UnityEngine.Object.Instantiate(busPrefab, preview.transform);
                    bus.transform.position = door + Vector3.left * (i * Field<float>(station, "queueSpacing"));
                    Freeze(bus.gameObject);
                    Set(bus, "animator", null);
                    var body = Field<Renderer>(bus, "body");
                    var shared = body.sharedMaterials;
                    shared[0] = materials[(int)level.busOrder[i]];
                    body.sharedMaterials = shared;
                    foreach (var indicator in Field<GameObject[]>(bus, "seatIndicators")) indicator.SetActive(false);
                    if (i == 0) activeBus = bus;
                }
                foreach (var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
                {
                    if (text.name == "LevelText" && text.transform.IsChildOf(GameObject.Find("Canvas").transform)) text.text = "Level 7";
                }
                if (screen == "Complete")
                {
                    boardView.gameObject.SetActive(false);
                    foreach (var bus in preview.GetComponentsInChildren<Bus>(true)) bus.gameObject.SetActive(false);
                    var complete = GameObject.Find("Canvas").GetComponentInChildren<BusJam.UI.CompletePanel>(true);
                    foreach (string field in new[] { "header", "rays", "pile" }) Field<RectTransform>(complete, field).localScale = Vector3.one;
                    Field<UnityEngine.UI.Button>(complete, "nextButton").transform.localScale = Vector3.one;
                    Field<TMP_Text>(complete, "rewardText").transform.localScale = Vector3.one;
                    Field<TMP_Text>(complete, "rewardText").text = "+25";
                    var dim = Field<UnityEngine.UI.Image>(complete, "dim");
                    var colour = dim.color; colour.a = .65f; dim.color = colour;
                    foreach (var coin in Field<UnityEngine.UI.Image[]>(complete, "coins")) coin.gameObject.SetActive(false);
                }
            }
            foreach (var t in preview.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
            RebuildUI();
            RepaintGameView();
            return "Prepared " + phase + " " + screen + "; Edit mode; 1080x1920; disposable Level_07 fixture.";
        }

        private static void Freeze(GameObject root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.Rebind();
                animator.Update(0);
                animator.enabled = false;
            }
        }

        private static void ConfigureGameView()
        {
            var assembly = typeof(Editor).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");
            gameView = EditorWindow.GetWindow(type);
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance", Flags).GetValue(null);
            var groupType = type.GetProperty("currentSizeGroupType", Flags).GetValue(gameView);
            var group = sizesType.GetMethod("GetGroup", Flags).Invoke(sizes, new[] { groupType });
            var groupClass = group.GetType();
            int count = (int)groupClass.GetMethod("GetTotalCount", Flags).Invoke(group, null);
            int index = -1;
            for (int i = 0; i < count; i++)
            {
                var size = groupClass.GetMethod("GetGameViewSize", Flags).Invoke(group, new object[] { i });
                if ((int)size.GetType().GetProperty("width", Flags).GetValue(size) == 1080 &&
                    (int)size.GetType().GetProperty("height", Flags).GetValue(size) == 1920) { index = i; break; }
            }
            if (index < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var enumType = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, Flags, null, new[] { Enum.Parse(enumType, "FixedResolution"), (object)1080, 1920, "Portrait 1080x1920" }, null);
                groupClass.GetMethod("AddCustomSize", Flags).Invoke(group, new[] { size });
                index = count;
            }
            type.GetProperty("selectedSizeIndex", Flags).SetValue(gameView, index);
            type.GetField("m_Gizmos", Flags).SetValue(gameView, false);
            type.GetField("m_ClearInEditMode", Flags).SetValue(gameView, false);
            Camera.main.aspect = 1080f / 1920;
        }

        private static void RebuildUI()
        {
            Canvas.ForceUpdateCanvases();
            foreach (var text in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
                if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate();
            foreach (var graphic in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Graphic>(true))
                if (graphic.gameObject.activeInHierarchy) { graphic.SetAllDirty(); graphic.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender); graphic.Rebuild(UnityEngine.UI.CanvasUpdate.LatePreRender); }
            Canvas.ForceUpdateCanvases();
        }

        private static RenderTexture Render()
        {
            RebuildUI();
            var rt = (RenderTexture)gameView.GetType().GetMethod("RenderView", Flags).Invoke(gameView, new object[] { Vector2.zero, false });
            typeof(EditorGUIUtility).GetMethod("RenderPlayModeViewCamerasInternal", Flags).Invoke(null,
                new object[] { rt, 0, Vector2.zero, false, false });
            return rt;
        }

        public static string Capture(string screen)
        {
            if (preview == null || EditorApplication.isPlaying) throw new InvalidOperationException("Prepare in Edit mode first.");
            RepaintGameView();
            Render(); Render();
            var rt = Render();
            var old = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = old;
            var pixels = texture.GetPixels32();
            if (SystemInfo.graphicsUVStartsAtTop)
            {
                var flipped = new Color32[pixels.Length];
                for (int y = 0; y < rt.height; y++) Array.Copy(pixels, y * rt.width, flipped, (rt.height - y - 1) * rt.width, rt.width);
                texture.SetPixels32(flipped); texture.Apply();
            }
            string path = "Docs/ZLIGHT-Captures/" + phase + "-" + screen + ".png";
            Directory.CreateDirectory("Docs/ZLIGHT-Captures");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            return path + " (" + rt.width + "x" + rt.height + ")";
        }

        private static Type DebuggerType => typeof(Editor).Assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility");

        public static string BeginFrame()
        {
            RebuildUI();
            gameView.Focus();
            DebuggerType.GetMethod("SetEnabled", Flags).Invoke(null, new object[] { true, 0 });
            RepaintGameView();
            return "Frame Debugger capture requested in Edit mode.";
        }

        public static string Calibration(int colour)
        {
            string name = ((PassengerColor)colour).ToString();
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BusJam/Materials/M_" + name + ".mat");
            foreach (var passenger in boardView.GetComponentsInChildren<Passenger>(true))
                Field<Renderer>(passenger, "body").sharedMaterial = material;
            var body = Field<Renderer>(activeBus, "body");
            var shared = body.sharedMaterials;
            shared[0] = material;
            body.sharedMaterials = shared;
            return Capture("Colour-" + name);
        }

        [Serializable] private class FrameRow { public int index; public string type, name; }
        [Serializable] private class FrameReport { public int events, draws, setPass, shadowCasters; public FrameRow[] rows; }

        public static string EndFrame(string screen)
        {
            RepaintGameView();
            var type = DebuggerType;
            var events = (Array)type.GetMethod("GetFrameEvents", Flags).Invoke(null, null);
            if (events.Length == 0) return "Frame pending: call EndFrame on the next editor tick.";
            var rows = new List<FrameRow>();
            for (int i = 0; i < events.Length; i++)
            {
                var evt = events.GetValue(i);
                var row = new FrameRow { index = i, type = evt.GetType().GetField("m_Type", Flags).GetValue(evt).ToString(), name = (string)type.GetMethod("GetFrameEventInfoName", Flags).Invoke(null, new object[] { i }) };
                rows.Add(row);
            }
            var report = new FrameReport { events = events.Length, draws = UnityStats.drawCalls, setPass = UnityStats.setPassCalls, shadowCasters = UnityStats.shadowCasters, rows = rows.ToArray() };
            string json = JsonUtility.ToJson(report, true);
            Directory.CreateDirectory("Docs/ZLIGHT-Captures");
            File.WriteAllText("Docs/ZLIGHT-Captures/" + phase + "-" + screen + "-Frame.json", json);
            type.GetMethod("SetEnabled", Flags).Invoke(null, new object[] { false, 0 });
            return "Frame events=" + report.events + ", UnityStats draws=" + report.draws + ", SetPass=" + report.setPass + ", shadow casters=" + report.shadowCasters;
        }

        private static void RepaintGameView()
        {
            gameView.Focus();
            var parent = typeof(EditorWindow).GetField("m_Parent", Flags).GetValue(gameView);
            parent.GetType().GetMethod("RepaintImmediately", Flags).Invoke(parent, null);
        }

        public static string Cleanup()
        {
            if (preview != null) UnityEngine.Object.DestroyImmediate(preview);
            preview = null; boardView = null; activeBus = null;
            EditorSceneManager.OpenScene("Assets/_BusJam/Scenes/Game.unity", OpenSceneMode.Single);
            return "Preview removed; saved Game scene restored; Edit mode.";
        }
    }

}
