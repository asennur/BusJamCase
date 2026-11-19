using System;
using System.Collections.Generic;
using System.Reflection;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.LevelEditor;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>ZM2b için tekrar çalıştırılabilen Edit kontrolleri; Play'e girmez, level assetlerini değiştirmez.</summary>
    public static class ZM2bVerification
    {
        private static int checks;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        public static string Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit modu gerekli.");
            checks = 0;
            SolverChecks();
            FailChecks();
            return $"{checks} Edit kontrolü geçti.";
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            checks++;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);
        private static object Get(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
        private static void Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);

        public static LevelData CloakTest()
        {
            var level = Line(3, 1);
            level.cells[2].cloak = Cloak.StartsClosed;
            level.timeLimit = 120;
            return level;
        }

        private static LevelData Line(int width, int height)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.width = width; level.height = height;
            level.cells = new CellData[width * height];
            for (int i = 0; i < level.cells.Length; i++)
                level.cells[i] = new CellData { type = CellType.Passenger, color = PassengerColor.Red };
            level.busOrder.Add(PassengerColor.Red);
            return level;
        }

        private static void SolverChecks()
        {
            var line = Line(1, 3);
            try
            {
                Check(LevelSolver.Solve(line, 1).SolutionCount == 1, "Dikey üçlü tek çözüm");
                line.cells[1].ropes = 1;
                Check(LevelSolver.Solve(line, 1).SolutionCount == 1, "Komşu ayrılırken ip çözülür");
                line.cells[1].ropes = 0; line.cells[0].ropes = 1;
                Check(LevelSolver.Solve(line, 1).SolutionCount == 0, "Ön sıradaki ip yolu kilitler");
                line.cells[0].ropes = 0;
                line.cells[0].cloak = Cloak.StartsClosed;
                Check(LevelSolver.Solve(line, 1).SolutionCount == 0, "Kapalı ön yolcu");
                line.cells[0].cloak = Cloak.None;
                Check(LevelSolver.Solve(line, 1, 1).Limited, "Durum sınırı");
                Check(LevelSolver.Solve(line, 1, 200000, 0).Limited, "Zaman sınırı");
            }
            finally { Object.DestroyImmediate(line); }

            var row = Line(3, 1);
            try
            {
                var result = LevelSolver.Solve(row, 1);
                Check(result.SolutionCount == 3 && !result.Limited, "Üç çözümde kesilir");
                var unique = new HashSet<string>();
                foreach (var sequence in result.Solutions) unique.Add(string.Join(",", sequence));
                Check(unique.Count == 3, "Olay zamanlamaları aynı dokunuş dizisini çoğaltmaz");
                for (int i = 0; i < 3; i++) row.cells[i].cloak = Cloak.StartsClosed;
                result = LevelSolver.Solve(row, 1);
                Check(result.SolutionCount == 0 && !result.Limited && result.Label == "Çözülemez", "Hepsi kapalı");
            }
            finally { Object.DestroyImmediate(row); }

            var cloak = CloakTest();
            try
            {
                var result = LevelSolver.Solve(cloak, 1, findLosingSequence: true);
                Check(result.SolutionCount >= 2 && result.LosingSequence != null, "Kazanan ve kilitlenen yol");
                Check(result.LosingSequence.Length == 2, "İki normalden sonra son pelerin kapalı");
                foreach (var sequence in result.Solutions)
                    Check(LevelSolver.ReplaySettled(cloak, 11, sequence) == "Complete", "Test kazanan sıra replay");
                Check(LevelSolver.ReplaySettled(cloak, 11, result.LosingSequence) == "NoMoves", "Test kilitlenen sıra replay");
            }
            finally { Object.DestroyImmediate(cloak); }

            var tunnel = Line(2, 2);
            try
            {
                tunnel.cells[1].type = CellType.Void;
                tunnel.cells[2].type = CellType.Empty;
                tunnel.cells[3].type = CellType.Tunnel;
                tunnel.tunnels.Add(new TunnelData { cell = new Vector2Int(1, 1), facing = Direction.Left,
                    queue = new List<PassengerColor> { PassengerColor.Red, PassengerColor.Red } });
                var result = LevelSolver.Solve(tunnel, 1);
                Check(result.SolutionCount == 1, "Tünel tekrarlanan ağız dokunuşu");
                Check(string.Join(",", result.Solutions[0]) == "0,2,2", "Tünel kuyruğu sıralı");
            }
            finally { Object.DestroyImmediate(tunnel); }

            var slots = Line(1, 9);
            try
            {
                for (int i = 0; i < 9; i++) slots.cells[i].color = i < 5 || i == 8 ? PassengerColor.Blue : PassengerColor.Red;
                slots.busOrder.Add(PassengerColor.Blue); slots.busOrder.Add(PassengerColor.Blue);
                Check(LevelSolver.Solve(slots, 49).SolutionCount == 0, "Beş dolu slot ZD2 fail");
                Check(LevelSolver.Solve(slots, 50).SolutionCount == 1, "50. levelde altıncı slot");
                Check(LevelSolver.Solve(slots, 100).SolutionCount == 1, "100. levelde yedinci slot");
            }
            finally { Object.DestroyImmediate(slots); }

            var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/_BusJam/Data/LevelDatabase.asset");
            Check(db.Count == 10, "Kalıcı database 10 level");
            for (int i = 0; i < db.Count; i++)
            {
                var level = db.Get(i);
                var result = LevelSolver.Solve(level, i + 1);
                Check(result.Error == null && !result.Limited && result.SolutionCount > 0, level.name + " çözülebilir");
                foreach (var sequence in result.Solutions)
                    Check(LevelSolver.ReplaySettled(level, i + 1, sequence) == "Complete", level.name + " kazanan sıra replay");
                foreach (var cell in level.cells)
                    if (cell.ropes > 0 || cell.cloak != Cloak.None)
                    { Check(result.SolutionCount >= 2, level.name + " mekanikli level ≥2"); break; }
            }
        }

        private static void FailChecks()
        {
            string[] lifeKeys = { "Lives", "LastRegenUtc", "LivesUtcHighWater" };
            bool[] existed = Array.ConvertAll(lifeKeys, PlayerPrefs.HasKey);
            string[] saved = Array.ConvertAll(lifeKeys, key => key == "Lives"
                ? PlayerPrefs.GetInt(key).ToString() : PlayerPrefs.GetString(key));
            var scene = EditorSceneManager.NewPreviewScene();
            var level = Line(3, 1);
            var spawned = new List<GameObject>();
            try
            {
                var systems = new GameObject("EditChecks");
                SceneManager.MoveGameObjectToScene(systems, scene);
                var controller = systems.AddComponent<GameplayController>();
                var manager = systems.AddComponent<GameManager>();
                Set(manager, "livesSystem", systems.AddComponent<LivesSystem>());
                var station = systems.AddComponent<BusStation>();
                var waiting = systems.AddComponent<WaitingArea>();
                Set(controller, "gameManager", manager);
                Set(controller, "busStation", station);
                Set(controller, "waitingArea", waiting);
                Set(manager, "levelTimer", systems.AddComponent<LevelTimer>());
                var board = new Board(); board.Setup(level);
                Set(controller, "board", board);
                var reachable = new[] { true, false, false }; Set(controller, "reachable", reachable);
                Set(manager, "<State>k__BackingField", GameState.Playing);

                var busPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BusJam/Prefabs/Bus.prefab");
                var busObject = Object.Instantiate(busPrefab); spawned.Add(busObject);
                SceneManager.MoveGameObjectToScene(busObject, scene);
                var bus = busObject.GetComponent<Bus>();
                Set(bus, "<Color>k__BackingField", PassengerColor.Red);
                bus.SetAtDoor(true);
                ((List<Bus>)Get(station, "queue")).Add(bus);
                bool[] unlocked = (bool[])Get(waiting, "unlocked");
                for (int i = 1; i < 6; i++) unlocked[i] = true;
                var slots = (Passenger[])Get(waiting, "slots");
                var passengerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_BusJam/Prefabs/Passenger.prefab");
                var passengerObject = Object.Instantiate(passengerPrefab); spawned.Add(passengerObject);
                SceneManager.MoveGameObjectToScene(passengerObject, scene);
                var passenger = passengerObject.GetComponent<Passenger>();
                Set(passenger, "animator", null);
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_BusJam/Materials/M_Red.mat");
                passenger.Setup(PassengerColor.Red, Vector2Int.zero, false, material, Cloak.StartsClosed);
                board.SetOccupant(Vector2Int.zero, passenger);
                int failures = 0;
                FailReason observed = FailReason.OutOfTime;
                controller.LevelFailed += reason => { failures++; observed = reason; };

                Action<bool, string> probe = (expectFail, name) =>
                {
                    Set(manager, "<State>k__BackingField", GameState.Playing);
                    int before = failures; Call(controller, "CheckFail");
                    Check((failures > before) == expectFail, name);
                };
                probe(true, "Son kapalı pelerin NoMoves"); Check(observed == FailReason.NoMoves, "NoMoves nedeni");
                Set(controller, "runningCount", 1); probe(false, "Koşu bitmeden fail yok");
                Set(controller, "runningCount", 0); bus.SetAtDoor(false); probe(false, "Otobüs kapıya varmadan fail yok");
                bus.SetAtDoor(true);
                ((List<Bus>)Get(station, "departing")).Add(bus); probe(false, "Kalkış sürerken fail yok");
                ((List<Bus>)Get(station, "departing")).Clear();
                slots[1] = passenger; probe(false, "Biniş bekleyen varsa fail yok"); slots[1] = null;
                var overflow = (List<Passenger>)Get(controller, "overflowQueue");
                overflow.Add(passenger); probe(false, "Taşma kuyruğu boşalmadan NoMoves yok"); overflow.Clear();
                passenger.SetCloak(true); probe(false, "Açık erişilebilir yolcu hamle sağlar");
                reachable[0] = false; probe(true, "Erişilemez yolcu NoMoves"); reachable[0] = true;
                passenger.SetRopes(1); probe(true, "İpli yolcu NoMoves"); passenger.SetRopes(0);
                board.SetOccupant(Vector2Int.zero, null); probe(false, "Grid boşken NoMoves yok");
                board.SetOccupant(Vector2Int.zero, passenger);
                Set(passenger, "<Color>k__BackingField", PassengerColor.Blue);
                for (int i = 1; i < 6; i++) slots[i] = passenger;
                probe(true, "Dolu yerleşmiş slotlar NoSpace"); Check(observed == FailReason.NoSpace, "NoSpace korunur");
                Array.Clear(slots, 0, slots.Length);
                Set(manager, "<State>k__BackingField", GameState.Start);
                int old = failures; Call(controller, "CheckFail"); Check(old == failures, "Start durumunda fail yok");
                var toText = typeof(BusJam.UI.FailPanel).GetMethod("ToText", BindingFlags.Static | BindingFlags.NonPublic);
                Check((string)toText.Invoke(null, new object[] { FailReason.NoMoves }) == "No moves left!", "FailPanel metni");
            }
            finally
            {
                foreach (var item in spawned) Object.DestroyImmediate(item);
                Object.DestroyImmediate(level);
                EditorSceneManager.ClosePreviewScene(scene);
                for (int i = 0; i < lifeKeys.Length; i++)
                {
                    if (!existed[i]) PlayerPrefs.DeleteKey(lifeKeys[i]);
                    else if (i == 0) PlayerPrefs.SetInt(lifeKeys[i], int.Parse(saved[i]));
                    else PlayerPrefs.SetString(lifeKeys[i], saved[i]);
                }
                PlayerPrefs.Save();
            }
        }

        private static T ComponentIn<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            throw new Exception(typeof(T).Name + " bulunamadı");
        }

        public static void BindSolverUI()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_BusJam/Scenes/LevelEditor.unity", OpenSceneMode.Additive);
            try
            {
                var panel = ComponentIn<LevelEditorPanel>(scene);
                var sourceButton = ComponentIn<Button>(scene);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var button in root.GetComponentsInChildren<Button>(true))
                        if (button.name == "Kaydet") sourceButton = button;
                var buttonObject = Object.Instantiate(sourceButton.gameObject, sourceButton.transform.parent);
                buttonObject.name = "Çöz";
                var rect = buttonObject.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(292, -556);
                var solve = buttonObject.GetComponent<Button>(); solve.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddPersistentListener(solve.onClick, panel.Solve);
                buttonObject.GetComponentInChildren<TMP_Text>().text = "Çöz";
                var status = (TMP_Text)Get(panel, "status");
                var result = Object.Instantiate(status, status.transform.parent);
                result.name = "SolverResult";
                result.rectTransform.anchoredPosition = new Vector2(-516, -606);
                result.rectTransform.sizeDelta = new Vector2(800, 100);
                result.text = "Çözüm kontrolü: Çöz düğmesine bas.";
                result.enableWordWrapping = true;
                result.overflowMode = TextOverflowModes.Overflow;
                Set(panel, "solverResult", result);
                EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
