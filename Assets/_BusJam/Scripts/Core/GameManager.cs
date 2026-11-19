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
        private int readyFrame;

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
            if (inputHandler != null)
                inputHandler.ScreenTapped += OnScreenTapped;
            if (levelTimer != null)
                levelTimer.Expired += OnTimerExpired;
        }

        private void OnDisable()
        {
            if (inputHandler != null)
                inputHandler.ScreenTapped -= OnScreenTapped;
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

        // Ready displays the full constraint; only Playing advances the timer.
        private void SetState(GameState newState)
        {
            if (newState != GameState.Playing && IsPaused) SetPaused(false);
            State = newState;

            if (newState == GameState.Playing)
                levelTimer.StartTimer(currentLevel.timeLimit);
            else if (newState == GameState.Ready)
            {
                readyFrame = Time.frameCount;
                levelTimer.PrepareTimer(currentLevel.timeLimit);
            }
            else
                levelTimer.StopTimer();

            StateChanged?.Invoke(State);
        }

        private void OnTimerExpired()
        {
            if (State == GameState.Playing)
                Fail(FailReason.OutOfTime);
        }

        public void PrepareLevel()
        {
            livesSystem.Refresh();
            if (State != GameState.Start || livesSystem.Lives == 0)
                return;

            SetState(GameState.Ready);
        }

        private void OnScreenTapped()
        {
            if (Time.frameCount > readyFrame)
                StartPlaying();
        }

        public void StartPlaying()
        {
            if (State != GameState.Ready) return;
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

        /// <summary>True while the in-game settings panel is open: time stops and taps are ignored.</summary>
        public bool IsPaused { get; private set; }

        public void SetPaused(bool paused)
        {
            if (paused && State != GameState.Playing) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        /// <summary>Restart from the settings panel: costs a life, then the level waits for Tap to start again.</summary>
        public void Restart()
        {
            if (State != GameState.Playing) return;
            SetPaused(false);
            if (!livesSystem.TrySpendLife()) return;
            Retry();
        }

        /// <summary>Main Menu from the settings panel: costs a life and returns to the start screen.</summary>
        public void QuitToMenu()
        {
            if (State != GameState.Playing) return;
            SetPaused(false);
            livesSystem.TrySpendLife();
            LoadLevel(currentLevelIndex);
            SetState(GameState.Start);
        }

        public void LoadNext()
        {
            LoadLevel(currentLevelIndex);
            PrepareLoadedLevel();
        }

        public void Retry()
        {
            LoadLevel(currentLevelIndex);
            PrepareLoadedLevel();
        }

        private void PrepareLoadedLevel()
        {
            livesSystem.Refresh();
            SetState(livesSystem.Lives > 0 ? GameState.Ready : GameState.Start);
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
