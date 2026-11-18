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

        private Board board;
        private GameState state;
        private bool stateInitialized;
        private int currentLevelIndex;
        private LevelData currentLevel;

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

            board = new Board();
            currentLevelIndex = SaveSystem.GetLevelIndex();
            LoadLevel(currentLevelIndex);
            SetState(GameState.Start);
        }

        private void OnEnable()
        {
            if (inputHandler != null)
            {
                inputHandler.ScreenTapped += OnScreenTapped;
                inputHandler.PassengerTapped += OnPassengerTapped;
            }
        }

        private void OnDisable()
        {
            if (inputHandler != null)
            {
                inputHandler.ScreenTapped -= OnScreenTapped;
                inputHandler.PassengerTapped -= OnPassengerTapped;
            }
        }

        private void LoadLevel(int index)
        {
            currentLevel = levelDatabase.Get(index);
            board.Setup(currentLevel);
            boardView.Build(currentLevel, board);
            LevelLoaded?.Invoke(currentLevelIndex, currentLevel);
            Debug.Log($"[GameManager] Level {currentLevelIndex} yüklendi: {currentLevel.name}");
        }

        // state'in varsayılanı zaten GameState.Start olduğundan, ilk çağrıyı atlamamak için ayrı bayrak gerekir.
        private void SetState(GameState newState)
        {
            if (stateInitialized && state == newState)
                return;

            stateInitialized = true;
            state = newState;
            Debug.Log($"[GameManager] Durum: {state}");
            StateChanged?.Invoke(state);
        }

        private void OnScreenTapped()
        {
            if (state == GameState.Start)
                StartPlaying();
        }

        // Geçici: dokunma kararını Z4'te GameplayController verecek.
        private void OnPassengerTapped(Passenger passenger)
        {
            Debug.Log($"[GameManager] Yolcuya dokunuldu: {passenger.Color} @ {passenger.Cell}");
        }

        public void StartPlaying()
        {
            if (state != GameState.Start)
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

        public GameState GetState() => state;
        public Board GetBoard() => board;
    }
}
