using System;
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
using Object = UnityEngine.Object;

namespace BusJam.Editor
{
    /// <summary>Checks ready routing and atomic rewards in an isolated Edit-mode fixture.</summary>
    public static class ZTSVerification
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int checks;
        private static void Check(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); checks++; }
        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static T Get<T>(object target, string field) => ZM3Setup.Get<T>(target, field);
        private static void Set(object target, string field, object value) => ZM3Setup.Set(target, field, value);

        public static string Run()
        {
            ZM3Setup.RequireEditMode();
            checks = 0;
            string[] keys = { "Coins", "LevelIndex", "Lives", "LastRegenUtc", "LivesUtcHighWater" };
            var existed = new bool[keys.Length];
            var ints = new int[3];
            var strings = new string[2];
            for (int i = 0; i < keys.Length; i++)
            { existed[i] = PlayerPrefs.HasKey(keys[i]); if (i < 3) ints[i] = PlayerPrefs.GetInt(keys[i]); else strings[i - 3] = PlayerPrefs.GetString(keys[i]); }
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject fixture = null, uiClone = null;
            LevelData level = null;
            LevelDatabase database = null;
            try
            {
                fixture = new GameObject("ZTSChecks");
                SceneManager.MoveGameObjectToScene(fixture, preview);
                var manager = fixture.AddComponent<GameManager>();
                var lives = fixture.AddComponent<LivesSystem>();
                var timer = fixture.AddComponent<LevelTimer>();
                var input = fixture.AddComponent<InputHandler>();
                var view = fixture.AddComponent<BoardView>();
                var gameplay = fixture.AddComponent<GameplayController>();
                Set(manager, "livesSystem", lives); Set(manager, "levelTimer", timer);
                Set(manager, "inputHandler", input); Set(manager, "boardView", view);
                Set(manager, "<Board>k__BackingField", new Board());
                Set(gameplay, "gameManager", manager);
                level = ScriptableObject.CreateInstance<LevelData>();
                level.width = level.height = 1; level.timeLimit = 30;
                level.cells = new[] { new CellData { type = CellType.Void } };
                database = ScriptableObject.CreateInstance<LevelDatabase>(); database.Add(level);
                Set(manager, "levelDatabase", database);
                Call(manager, "LoadLevel", 0);
                lives.ResetFull();
                uiClone = Object.Instantiate(ZM3Setup.InScene<UIManager>(SceneManager.GetActiveScene()).gameObject);
                SceneManager.MoveGameObjectToScene(uiClone, preview);
                var ui = uiClone.GetComponent<UIManager>();
                Set(ui, "gameManager", manager); Set(ui, "livesSystem", lives);
                Call(ui, "OnEnable"); Call(manager, "OnEnable");
                Call(manager, "SetState", GameState.Start);
                int starts = 0;
                manager.PlayingStarted += () => starts++;
                manager.PrepareLevel();
                Check(manager.State == GameState.Ready && !timer.enabled && timer.SecondsRemaining == 30, "Menu enters Ready with a full stopped timer");
                Check(!Get<GameObject>(ui, "startPanel").activeSelf && Get<GameObject>(ui, "hudPanel").activeSelf && Get<GameObject>(ui, "readyPanel").activeSelf, "Ready shows HUD and overlay");
                Check(starts == 0, "Tutorial start event waits for the ready tap");
                var screenTap = Get<Action>(input, "ScreenTapped");
                screenTap();
                Check(manager.State == GameState.Ready, "Menu activation frame cannot also start play");
                var passenger = fixture.AddComponent<Passenger>();
                Call(gameplay, "OnPassengerTapped", passenger);
                Check(passenger.State == PassengerState.Idle, "Passenger tap is ignored in Ready");
                Set(manager, "readyFrame", -1); screenTap();
                Check(manager.State == GameState.Playing && timer.enabled && starts == 1, "Next screen tap starts the timer and tutorial once");
                Check(!Get<GameObject>(ui, "readyPanel").activeSelf && Get<GameObject>(ui, "hudPanel").activeSelf, "Playing removes the overlay");
                screenTap(); Check(starts == 1, "Later screen taps do not restart play");
                manager.Fail(FailReason.OutOfTime); int remainingLives = lives.Lives;
                manager.Retry();
                Check(manager.State == GameState.Ready && timer.SecondsRemaining == 30 && !timer.enabled && lives.Lives == remainingLives, "Retry resets Ready without another life charge");
                manager.StartPlaying(); manager.Restart();
                Check(manager.State == GameState.Ready && lives.Lives == remainingLives - 1 && !timer.enabled, "Restart spends once and returns Ready");
                while (lives.Lives > 0) lives.TrySpendLife();
                manager.Retry(); manager.PrepareLevel();
                Check(manager.State == GameState.Start && Get<GameObject>(ui, "noLivesPanel").activeSelf && !Get<GameObject>(ui, "readyPanel").activeSelf, "Zero lives routes to NoLives");
                lives.ResetFull();
                manager.LoadNext();
                Check(manager.State == GameState.Ready && !timer.enabled, "Next enters Ready");

                var complete = uiClone.GetComponentInChildren<CompletePanel>(true);
                var counter = Get<CoinCounter>(complete, "coinCounter");
                Set(complete, "gameManager", manager);
                Call(counter, "Awake"); Call(complete, "Awake");
                PlayerPrefs.SetInt("Coins", 100); counter.Show(100);
                Call(complete, "ResetPose");
                Check(Get<TMP_Text>(complete, "rewardText").text == "+25", "Reward text is immediately +25");
                Get<Sequence>(complete, "entry").Goto(.15f);
                Check(Get<RectTransform>(complete, "pile").localScale.x > 0 && Get<TMP_Text>(complete, "rewardText").text == "+25", "Reward pops immediately without counting");
                Call(complete, "Continue");
                var transfer = Get<Sequence>(complete, "transfer");
                transfer.Goto(transfer.Duration() - .01f);
                Check(SaveSystem.Coins == 100 && Get<TMP_Text>(counter, "countText").text == "100", "Earlier coin arrivals do not change saved or displayed total");
                transfer.Complete(true);
                Check(SaveSystem.Coins == 125 && Get<TMP_Text>(counter, "countText").text == "125", "Last coin credits and displays the entire reward");
                Check(Get<Tween>(counter, "punch").IsPlaying(), "Final total receives a punch");
                Check(!(bool)Call(complete, "CreditReward") && SaveSystem.Coins == 125, "Reward cannot be saved twice");
                Check(manager.State == GameState.Ready && !timer.enabled && timer.SecondsRemaining == 30, "Reward Continue loads the next level in Ready");
                Call(ui, "OnDisable"); Call(manager, "OnDisable");
                return checks + " Edit-mode checks passed. Play remained off.";
            }
            finally
            {
                if (uiClone != null)
                {
                    var complete = uiClone.GetComponentInChildren<CompletePanel>(true);
                    if (complete != null) Call(complete, "OnDestroy");
                    foreach (var counter in uiClone.GetComponentsInChildren<CoinCounter>(true)) Call(counter, "OnDestroy");
                    Object.DestroyImmediate(uiClone);
                }
                if (fixture != null) Object.DestroyImmediate(fixture);
                EditorSceneManager.ClosePreviewScene(preview);
                if (level != null) Object.DestroyImmediate(level);
                if (database != null) Object.DestroyImmediate(database);
                for (int i = 0; i < keys.Length; i++)
                { if (!existed[i]) PlayerPrefs.DeleteKey(keys[i]); else if (i < 3) PlayerPrefs.SetInt(keys[i], ints[i]); else PlayerPrefs.SetString(keys[i], strings[i - 3]); }
                PlayerPrefs.Save();
            }
        }
    }
}
