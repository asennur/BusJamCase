using System;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.Save;
using UnityEngine;

namespace BusJam.Core
{
    /// <summary>
    /// Oyun akışını yönetir: durum makinesi, level yükleme, kaydetme.
    /// SaveSystem'den son leveli alır, level indeksini artırıp kaydeder.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private LevelDatabase levelDatabase;
        [SerializeField] private BoardView boardView;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private LevelTimer levelTimer;

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
            if (levelDatabase == null || boardView == null || inputHandler == null || levelTimer == null)
            {
                Debug.LogError("[GameManager] LevelDatabase, BoardView, InputHandler, LevelTimer referansları eksik");
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            Board = new Board();
        }

        // İlk level yükleme burada: Awake/OnEnable sırası nesneler arasında garanti değil,
        // UI ve GameplayController Start'a kadar event'lere abone olmuş olur.
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

        // Durum değişimleri zaten bilinçli çağrılır; Retry'de Start'ın yeniden atılması UI'ın sıfırlanması için istenir.
        // Timer yalnız Playing'de çalışır; diğer durumlara geçişte durur.
        private void SetState(GameState newState)
        {
            State = newState;

            if (newState == GameState.Playing)
                levelTimer.StartTimer(currentLevel.timeLimit);
            else
                levelTimer.StopTimer();

            StateChanged?.Invoke(State);
        }

        private void OnScreenTapped()
        {
            if (State == GameState.Start)
                StartPlaying();
        }

        private void OnTimerExpired()
        {
            if (State == GameState.Playing)
                Fail(FailReason.OutOfTime);
        }

        public void StartPlaying()
        {
            if (State != GameState.Start)
                return;

            SetState(GameState.Playing);
            PlayingStarted?.Invoke();
        }

        // İndeks ve kayıt kazanınca hemen ilerler; LoadNext yalnız o (zaten ilerlemiş) leveli sahneye kurar.
        public void Complete()
        {
            currentLevelIndex++;
            SaveSystem.SetLevelIndex(currentLevelIndex);
            SetState(GameState.Complete);
        }

        public void Fail(FailReason reason)
        {
            FailReason = reason;
            SetState(GameState.Fail);
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
    }
}
