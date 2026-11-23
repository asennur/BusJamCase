using System;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.Save;
using UnityEngine;

namespace BusJam.Core
{
    /// <summary>
    /// Oyun durumunu, level yüklemeyi ve kayıtlı ilerlemeyi yönetir.
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
        /// <summary>UI level geçişi sırasında true: ekran dokunuşları oyunu başlatamaz.</summary>
        public bool InputLocked { get; private set; }

        public event Action<GameState> StateChanged;
        public event Action PlayingStarted;
        public event Action PlayBlockedNoLives;
        public event Action<int, LevelData> LevelLoaded;

        /// <summary>Level editörünün Test Play'i: doluysa database yerine bu taslak kopyası oynanır, ilerleme kaydedilmez.</summary>
        public static LevelData TestLevel;
        public static int TestLevelNumber;

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

        // UI ve oynanış, ilk level Start'ta yüklenmeden önce abone olur.
        private void Start()
        {
            currentLevelIndex = TestLevel != null ? TestLevelNumber - 1 : SaveSystem.GetLevelIndex();
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
            currentLevel = TestLevel != null ? TestLevel : levelDatabase.Get(index);
            Board.Setup(currentLevel);
            boardView.Build(currentLevel, Board);
            CurrentLevelNumber = currentLevelIndex + 1;
            LevelLoaded?.Invoke(CurrentLevelNumber, currentLevel);
        }

        // Ready tam süreyi gösterir; sayacı yalnız Playing ilerletir.
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
            if (State != GameState.Start) return;
            if (livesSystem.Lives == 0)
            {
                PlayBlockedNoLives?.Invoke();
                return;
            }

            SetState(GameState.Ready);
        }

        private void OnScreenTapped()
        {
            if (!InputLocked && Time.frameCount > readyFrame)
                StartPlaying();
        }

        // Kilit açılması aynı karedeki dokunuşu da yok sayar.
        public void SetInputLocked(bool locked)
        {
            InputLocked = locked;
            if (!locked) readyFrame = Time.frameCount;
        }

        public void StartPlaying()
        {
            if (State != GameState.Ready) return;
            SetState(GameState.Playing);
            PlayingStarted?.Invoke();
        }

        // Kazanınca sonraki index hemen kaydedilir; LoadNext o kayıtlı leveli kurar.
        public void Complete()
        {
            if (TestLevel == null)
            {
                currentLevelIndex++;
                SaveSystem.SetLevelIndex(currentLevelIndex);
            }
            SetState(GameState.Complete);
        }

        public void Fail(FailReason reason)
        {
            if (State != GameState.Playing) return;
            livesSystem.TrySpendLife();
            FailReason = reason;
            SetState(GameState.Fail);
        }

        /// <summary>Oyun içi ayarlar paneli açıkken true: zaman durur, dokunuşlar yok sayılır.</summary>
        public bool IsPaused { get; private set; }

        // Duraklatma ekranı sabit; 30 FPS uzun oturumda ısınmayı ve pil tüketimini azaltır.
        public void SetPaused(bool paused)
        {
            if (paused && State != GameState.Playing) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            Application.targetFrameRate = paused ? 30 : 60;
        }

        /// <summary>Ayarlar panelinden Restart: bir can harcar, sonra level yeniden başlamak için Tap bekler.</summary>
        public void Restart()
        {
            if (State != GameState.Playing) return;
            SetPaused(false);
            if (!livesSystem.TrySpendLife()) return;
            Retry();
        }

        /// <summary>Ayarlar panelinden Main Menu: bir can harcar ve başlangıç ekranına döner.</summary>
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
