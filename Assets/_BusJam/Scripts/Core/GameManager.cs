using System;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.Save;
using UnityEngine;

namespace BusJam.Core
{
    /// <summary>
    /// Controls game state, level loading and saved progress.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private LevelDatabase levelDatabase;
        [SerializeField] private BoardView boardView;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private LevelTimer levelTimer;
        [SerializeField] private LivesSystem livesSystem;

        private int currentLevelIndex;
        private LevelData currentLevel;

        public GameState State { get; private set; }
        public Board Board { get; private set; }
        public FailReason FailReason { get; private set; }
        public LevelData CurrentLevel => currentLevel;
        public int CurrentLevelNumber { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action PlayingStarted;
        public event Action<int, LevelData> LevelLoaded;

        private void Awake()
        {
            if (levelDatabase == null || boardView == null || inputHandler == null || levelTimer == null || livesSystem == null)
            {
                Debug.LogError("[GameManager] Assign LevelDatabase, BoardView, InputHandler, LevelTimer and LivesSystem.");
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            Board = new Board();
        }

        // UI and gameplay subscribe before the first level is loaded in Start.
        private void Start()
        {
            currentLevelIndex = SaveSystem.GetLevelIndex();
            LoadLevel(currentLevelIndex);
            SetState(GameState.Start);
        }

        private void OnEnable()
        {
            if (levelTimer != null)
                levelTimer.Expired += OnTimerExpired;
        }

        private void OnDisable()
        {
            if (levelTimer != null)
                levelTimer.Expired -= OnTimerExpired;
        }

        private void LoadLevel(int index)
        {
            currentLevel = levelDatabase.Get(index);
            Board.Setup(currentLevel);
            boardView.Build(currentLevel, Board);
            CurrentLevelNumber = currentLevelIndex + 1;
            LevelLoaded?.Invoke(CurrentLevelNumber, currentLevel);
        }

        // Retrying publishes Start again so the UI resets; the timer runs only while Playing.
        private void SetState(GameState newState)
        {
            State = newState;

            if (newState == GameState.Playing)
                levelTimer.StartTimer(currentLevel.timeLimit);
            else
                levelTimer.StopTimer();

            StateChanged?.Invoke(State);
        }

        private void OnTimerExpired()
        {
            if (State == GameState.Playing)
                Fail(FailReason.OutOfTime);
        }

        public void StartPlaying()
        {
            livesSystem.Refresh();
            if (State != GameState.Start || livesSystem.Lives == 0)
                return;

            SetState(GameState.Playing);
            PlayingStarted?.Invoke();
        }

        // Winning saves the next index immediately; LoadNext builds that saved level.
        public void Complete()
        {
            currentLevelIndex++;
            SaveSystem.SetLevelIndex(currentLevelIndex);
            SetState(GameState.Complete);
        }

        public void Fail(FailReason reason)
        {
            if (State != GameState.Playing) return;
            livesSystem.TrySpendLife();
            FailReason = reason;
            SetState(GameState.Fail);
        }

        public void Restart()
        {
            if (State != GameState.Playing || !livesSystem.TrySpendLife()) return;
            Retry();
        }

        public void LoadNext()
        {
            LoadLevel(currentLevelIndex);
            SetState(GameState.Start);
        }

        public void Retry()
        {
            LoadLevel(currentLevelIndex);
            SetState(GameState.Start);
        }

        public void ResetProgress()
        {
            if (State != GameState.Start) return;
            SaveSystem.ResetProgress();
            livesSystem.ResetFull();
            currentLevelIndex = 0;
            LoadLevel(0);
            SetState(GameState.Start);
        }
    }
}
