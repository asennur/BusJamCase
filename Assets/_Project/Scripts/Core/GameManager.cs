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

        private int currentLevelIndex;
        private LevelData currentLevel;

        public GameState State { get; private set; }
        public Board Board { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action<int, LevelData> LevelLoaded;

        private void Awake()
        {
            if (levelDatabase == null || boardView == null || inputHandler == null)
            {
                Debug.LogError("[GameManager] LevelDatabase, BoardView, InputHandler referansları eksik");
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
        }

        private void OnDisable()
        {
            if (inputHandler != null)
                inputHandler.ScreenTapped -= OnScreenTapped;
        }

        private void LoadLevel(int index)
        {
            currentLevel = levelDatabase.Get(index);
            Board.Setup(currentLevel);
            boardView.Build(currentLevel, Board);
            int levelNumber = currentLevelIndex + 1;
            LevelLoaded?.Invoke(levelNumber, currentLevel);
            Debug.Log($"[GameManager] Level {levelNumber} yüklendi: {currentLevel.name}");
        }

        // Durum değişimleri zaten bilinçli çağrılır; Retry'de Start'ın yeniden atılması UI'ın sıfırlanması için istenir.
        private void SetState(GameState newState)
        {
            State = newState;
            Debug.Log($"[GameManager] Durum: {State}");
            StateChanged?.Invoke(State);
        }

        private void OnScreenTapped()
        {
            if (State == GameState.Start)
                StartPlaying();
        }

        public void StartPlaying()
        {
            if (State != GameState.Start)
                return;

            SetState(GameState.Playing);
        }

        public void LoadNext()
        {
            currentLevelIndex++;
            SaveSystem.SetLevelIndex(currentLevelIndex);
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
