using System;
using System.Collections.Generic;
using System.Reflection;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Exercises production life accounting and UI routing in a disposable Edit-mode scene.</summary>
    public static class ZM3Verification
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int checks;
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }

        private static void Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);

        private static void Set(object target, string field, object value) => ZM3Setup.Set(target, field, value);
        private static T Get<T>(object target, string field) => ZM3Setup.Get<T>(target, field);

        public static string Run()
        {
            ZM3Setup.RequireEditMode();
            checks = 0;
            string[] keys = { "Lives", "LastRegenUtc", "LivesUtcHighWater", "LevelIndex" };
            var existed = new bool[keys.Length];
            var values = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                existed[i] = PlayerPrefs.HasKey(keys[i]);
                values[i] = i == 0 || i == 3 ? PlayerPrefs.GetInt(keys[i]).ToString() : PlayerPrefs.GetString(keys[i]);
            }
            var preview = EditorSceneManager.NewPreviewScene();
            var go = new GameObject("LivesChecks");
            SceneManager.MoveGameObjectToScene(go, preview);
            var lives = go.AddComponent<LivesSystem>();
            LevelData level = null;
            LevelDatabase db = null;
            BoardView boardView = null;
            try
            {
                long now = DateTime.UtcNow.Ticks;
                long minute = TimeSpan.TicksPerMinute;
                Action<int, long, int> seed = (count, timestamp, interval) =>
                {
                    Set(lives, "initialized", false);
                    Set(lives, "rollbackUntilUtc", 0L);
                    Set(lives, "publishedLives", -1);
                    Set(lives, "publishedSeconds", -1);
                    Set(lives, "regenMinutes", interval);
                    PlayerPrefs.SetInt("Lives", count);
                    PlayerPrefs.SetString("LastRegenUtc", timestamp.ToString());
                    PlayerPrefs.SetString("LivesUtcHighWater", timestamp.ToString());
                    Call(lives, "Initialize", now);
                };
                PlayerPrefs.DeleteKey("Lives"); PlayerPrefs.DeleteKey("LastRegenUtc"); PlayerPrefs.DeleteKey("LivesUtcHighWater");
                Call(lives, "Initialize", now);
                Check(lives.Lives == 5 && lives.NextLifeSeconds == 0, "Fresh install must have five lives.");
                Check(lives.TrySpendLife() && lives.Lives == 4 && lives.NextLifeSeconds == 1200, "First loss starts a full 20-minute interval.");
                seed(1, now - 2 * minute, 1);
                Check(lives.Lives == 3 && lives.NextLifeSeconds == 60, "Two offline minutes award two lives.");
                seed(4, now - 240 * minute, 1);
                Check(lives.Lives == 5 && lives.NextLifeSeconds == 0, "Offline award caps at five.");
                seed(2, now - minute - TimeSpan.TicksPerSecond * 30, 1);
                Check(lives.Lives == 3 && lives.NextLifeSeconds == 30, "Partial interval survives regeneration.");
                seed(0, now, 1);
                Call(lives, "RefreshAt", now + minute - 1);
                Check(lives.Lives == 0 && lives.NextLifeSeconds == 1, "No early award before the exact boundary.");
                Call(lives, "RefreshAt", now + minute);
                Check(lives.Lives == 1 && lives.NextLifeSeconds == 60, "Exact boundary awards one life.");
                seed(0, now, 1);
                Check(!lives.TrySpendLife() && lives.Lives == 0, "Spending cannot underflow.");
                seed(5, now - 120 * minute, 1);
                Check(lives.TrySpendLife() && lives.NextLifeSeconds == 60, "Time at full capacity is not banked.");
                seed(1, now, 1);
                int events = 0;
                Action<int, int> changed = (count, seconds) => events++;
                lives.Changed += changed;
                Call(lives, "RefreshAt", now + 100);
                Check(events == 0, "Same displayed second emits no event.");
                Call(lives, "RefreshAt", now + TimeSpan.TicksPerSecond);
                Check(events == 1, "A new displayed second emits one event.");
                lives.Changed -= changed;
                var poll = (Action<long>)Delegate.CreateDelegate(typeof(Action<long>), lives,
                    typeof(LivesSystem).GetMethod("RefreshAt", Private));
                seed(1, now, 1);
                poll(now);
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) poll(now + i);
                Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "A thousand unchanged-second polls allocate zero bytes.");

                seed(1, now, 1);
                Call(lives, "RefreshAt", now - 60 * minute);
                Check(lives.Lives == 1 && lives.NextLifeSeconds == 60, "Rollback grants no lives and leaves a bounded countdown.");
                Check(PlayerPrefs.GetString("LastRegenUtc") == (now - 60 * minute).ToString(), "Rollback rebases LastRegenUtc to now.");
                Call(lives, "RefreshAt", now);
                Check(lives.Lives == 1 && lives.NextLifeSeconds == 60, "Restoring the clock cannot claim the rolled-back hour.");
                Call(lives, "RefreshAt", now + minute);
                Check(lives.Lives == 2, "Normal recovery resumes after the guard.");
                seed(1, now, 1);
                Call(lives, "RefreshAt", now - 60 * minute);
                Set(lives, "initialized", false); Set(lives, "rollbackUntilUtc", 0L);
                Call(lives, "Initialize", now - 30 * minute);
                Check(lives.Lives == 1, "Rollback guard survives an application restart.");
                Call(lives, "RefreshAt", now + minute * 2);
                Check(lives.Lives == 1, "Forward rebound after restart awards nothing.");
                Call(lives, "RefreshAt", now + minute * 3);
                Check(lives.Lives == 2, "A complete interval after rebound awards normally.");
                seed(2, now, 1);
                Set(lives, "lastRegenUtc", now - minute * 2);
                Set(lives, "lastObservedUtc", now - minute * 2);
                Call(lives, "OnApplicationPause", false);
                Check(lives.Lives == 4, "Resume calculates elapsed time immediately.");
                PlayerPrefs.SetString("LastRegenUtc", "invalid"); PlayerPrefs.SetString("LivesUtcHighWater", "invalid");
                PlayerPrefs.SetInt("Lives", -7); Set(lives, "initialized", false);
                Call(lives, "Initialize", now);
                Check(lives.Lives == 0 && lives.NextLifeSeconds == 60, "Malformed saves recover with a clamped count.");

                var manager = go.AddComponent<GameManager>();
                var timer = go.AddComponent<LevelTimer>();
                boardView = go.AddComponent<BoardView>();
                level = ScriptableObject.CreateInstance<LevelData>();
                level.width = level.height = 1;
                level.cells = new[] { new CellData { type = CellType.Void } };
                level.timeLimit = 30;
                db = ScriptableObject.CreateInstance<LevelDatabase>(); db.Add(level);
                Set(manager, "levelDatabase", db); Set(manager, "levelTimer", timer);
                Set(manager, "boardView", boardView); Set(manager, "livesSystem", lives);
                Set(manager, "<Board>k__BackingField", new Board());
                Set(manager, "currentLevel", level);
                seed(5, now, 1);
                for (int i = 4; i >= 0; i--)
                {
                    Set(manager, "<State>k__BackingField", GameState.Playing);
                    manager.Fail(FailReason.OutOfTime);
                    Check(lives.Lives == i && manager.State == GameState.Fail, "Fail must consume exactly one life.");
                    manager.Fail(FailReason.NoSpace);
                    Check(lives.Lives == i, "Duplicate Fail cannot charge twice.");
                    manager.Retry();
                    Check(lives.Lives == i && manager.State == GameState.Start, "Fail Retry must not charge another life.");
                }
                manager.StartPlaying();
                Check(manager.State == GameState.Start, "Zero lives must block level start.");

                var ui = go.AddComponent<UIManager>();
                Set(ui, "gameManager", manager); Set(ui, "livesSystem", lives);
                foreach (string field in new[] { "startPanel", "hudPanel", "completePanel", "failPanel", "noLivesPanel" })
                {
                    var panel = new GameObject(field); panel.transform.SetParent(go.transform);
                    Set(ui, field, panel);
                }
                Call(ui, "OnEnable");
                Call(ui, "OnStateChanged", GameState.Start);
                Check(Get<GameObject>(ui, "noLivesPanel").activeSelf && !Get<GameObject>(ui, "startPanel").activeSelf, "Zero lives selects NoLivesPanel.");
                Call(lives, "RefreshAt", DateTime.UtcNow.Ticks + minute);
                Check(!Get<GameObject>(ui, "noLivesPanel").activeSelf && Get<GameObject>(ui, "startPanel").activeSelf, "Recovery returns to StartPanel through the event.");
                seed(2, DateTime.UtcNow.Ticks, 1);
                manager.StartPlaying();
                Check(manager.State == GameState.Playing && lives.Lives == 2, "Starting does not spend a life.");
                manager.Restart();
                Check(lives.Lives == 1 && manager.State == GameState.Start, "Restart spends one life and retries.");
                manager.Restart();
                Check(lives.Lives == 1, "Restart outside Playing is ignored.");
                manager.StartPlaying(); manager.Restart();
                Check(lives.Lives == 0 && Get<GameObject>(ui, "noLivesPanel").activeSelf, "Restarting the last life shows NoLivesPanel.");
                Call(ui, "OnDisable");
                SceneChecks();
                return $"{checks} Edit checks passed. PlayerPrefs restored; Play mode was never entered.";
            }
            finally
            {
                if (boardView != null)
                {
                    var mesh = Get<Mesh>(boardView, "baseMesh");
                    Set(boardView, "baseMesh", null);
                    if (mesh != null) Object.DestroyImmediate(mesh);
                }
                EditorSceneManager.ClosePreviewScene(preview);
                if (level != null) Object.DestroyImmediate(level);
                if (db != null) Object.DestroyImmediate(db);
                for (int i = 0; i < keys.Length; i++)
                {
                    if (!existed[i]) PlayerPrefs.DeleteKey(keys[i]);
                    else if (i == 0 || i == 3) PlayerPrefs.SetInt(keys[i], int.Parse(values[i]));
                    else PlayerPrefs.SetString(keys[i], values[i]);
                }
                PlayerPrefs.Save();
            }
        }

        private static void SceneChecks()
        {
            var scene = SceneManager.GetActiveScene();
            var manager = ZM3Setup.InScene<GameManager>(scene);
            var lives = ZM3Setup.InScene<LivesSystem>(scene);
            var ui = ZM3Setup.InScene<UIManager>(scene);
            var hud = ZM3Setup.InScene<HudPanel>(scene);
            Check(Get<LivesSystem>(manager, "livesSystem") == lives && Get<LivesSystem>(ui, "livesSystem") == lives, "Scene life references must point to the same system.");
            Check(Get<int>(lives, "regenMinutes") == 20 && lives.MaxLives == 5, "Production scene uses 5 lives / 20 minutes.");
            Check(Get<UnityEngine.UI.Button>(hud, "restartButton") != null, "HUD Restart button is wired.");
            Check(Get<GameObject>(ui, "noLivesPanel") != null, "NoLivesPanel is wired.");
            var displays = ui.GetComponentsInChildren<LivesDisplay>(true);
            Check(displays.Length == 3, "HUD, Start and NoLives each have a life display.");
            foreach (var display in displays)
            {
                Check(Get<LivesSystem>(display, "livesSystem") == lives && Get<TMP_Text>(display, "countText") != null
                    && Get<TMP_Text>(display, "countdownText") != null && Get<GameObject>(display, "countdownGroup") != null, "All display fields must be assigned.");
                var clone = Object.Instantiate(display);
                try
                {
                    Call(clone, "Show", 0, 754);
                    Check(Get<TMP_Text>(clone, "countdownText").text.EndsWith("12:34"), "Countdown renders mm:ss.");
                    Call(clone, "Show", 5, 0);
                    Check(!Get<GameObject>(clone, "countdownGroup").activeSelf, "Full lives hide the countdown.");
                    var show = (Action<int, int>)Delegate.CreateDelegate(typeof(Action<int, int>), clone,
                        typeof(LivesDisplay).GetMethod("Show", Private));
                    for (int i = 0; i < 60; i++) show(3, 1200 - i);
                    long allocated = GC.GetAllocatedBytesForCurrentThread();
                    for (int i = 0; i < 1000; i++) show(3, 1200 - i % 60);
                    Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "A thousand warmed countdown updates allocate zero bytes.");
                }
                finally { Object.DestroyImmediate(clone.gameObject); }
            }
            Check(!System.IO.Directory.Exists("Assets/_BusJam/Levels/_Test") && !System.IO.Directory.Exists("Assets/_BusJam/Data/Levels/_Test"), "No temporary test folder remains.");
            Check(AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/_BusJam/Data/LevelDatabase.asset").Count == 10, "Permanent level database remains unchanged.");
        }
    }
}
