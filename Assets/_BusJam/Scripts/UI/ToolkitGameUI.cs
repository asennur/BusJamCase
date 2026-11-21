using System.Text;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.Save;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    /// <summary>Owns the runtime UI Toolkit screens and their event bindings.</summary>
    [ExecuteAlways, RequireComponent(typeof(UIDocument))]
    public sealed partial class ToolkitGameUI : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LivesSystem livesSystem;
        [SerializeField] private LevelTimer levelTimer;
        [SerializeField] private LayoutModeController layoutMode;

        private VisualElement documentRoot, root, background, start, hud, ready, readyDim;
        private VisualElement settings, resetConfirmation, pause, pauseConfirmation;
        private Button startButton, startSettings, pauseSettings, settingsClose, resetButton, resetYes, resetNo;
        private Button pauseClose, restartButton, menuButton, confirmYes, confirmNo;
        private Label levelNumber, timerText, livesCount, lifeCountdown, coinCount, readyPrompt, confirmText;
        private Toggle soundToggle;
        private readonly StringBuilder textBuffer = new StringBuilder(32);
        private Tween startPulse, readyPulse, urgentPulse, coinPulse;
        private GameState shownState;
        private enum PendingAction { None, Restart, MainMenu }
        private PendingAction pending;
        private bool subscribed;
        // UIDocument builds its tree in its own OnEnable; Unity does not order sibling OnEnable calls,
        // so after a domain reload ours can run first. Bind on the first Update that sees the tree.
        private bool waitingForTree;
        private int lastTime = -1, lastLives = -1, lastLifeSeconds = -1, lastCoins = -1;

        private void OnEnable()
        {
            if (document == null || gameManager == null || livesSystem == null || levelTimer == null || layoutMode == null) return;
            if (!CacheElements()) { waitingForTree = true; return; }
            waitingForTree = false;
            BindResults(true);
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            root.RegisterCallback<ClickEvent>(OnButtonSound, TrickleDown.TrickleDown);
            layoutMode.Changed += ApplyLayout;
            BindButtons(true);
            if (Application.isPlaying)
            {
                gameManager.StateChanged += DisplayState;
                gameManager.LevelLoaded += ShowLevel;
                levelTimer.TimeChanged += ShowTime;
                livesSystem.Changed += ShowLives;
                SaveSystem.CoinsChanged += ShowCoins;
                gameManager.PlayBlockedNoLives += ShowNoLives;
                subscribed = true;
                BuildTweens();
                livesSystem.Refresh();
            }
            lastTime = lastLives = lastLifeSeconds = lastCoins = -1;
            ShowLevel(Mathf.Max(1, gameManager.CurrentLevelNumber), gameManager.CurrentLevel);
            ShowTime(levelTimer.SecondsRemaining);
            ShowLives(Application.isPlaying ? livesSystem.Lives : livesSystem.MaxLives, livesSystem.NextLifeSeconds);
            ShowCoins(SaveSystem.Coins);
            DisplayState(Application.isPlaying ? gameManager.State : GameState.Start);
            ApplyLayout(layoutMode.Mode);
        }

        private void Update()
        {
            if (waitingForTree && document.rootVisualElement?.Q("ui-root") != null) OnEnable();
        }

        /// <summary>Returns false until the UIDocument tree exists; never binds to a half-built tree.</summary>
        private bool CacheElements()
        {
            var current = document.rootVisualElement;
            if (current == null || current.Q("ui-root") == null) return false;
            if (documentRoot == current && root != null) return true;
            KillTweens();
            documentRoot = current;
            documentRoot.pickingMode = PickingMode.Ignore;
            root = documentRoot.Q("ui-root");
            VectorArt.Apply(root);
            background = root.Q("menu-background");
            start = root.Q("start-screen"); hud = root.Q("hud");
            ready = root.Q("ready-overlay"); readyDim = root.Q("ready-dim");
            settings = root.Q("settings-overlay"); resetConfirmation = root.Q("reset-confirmation");
            pause = root.Q("pause-overlay"); pauseConfirmation = root.Q("pause-confirmation");
            startButton = root.Q<Button>("start-button"); startSettings = root.Q<Button>("start-settings");
            pauseSettings = root.Q<Button>("pause-settings"); settingsClose = root.Q<Button>("settings-close");
            resetButton = root.Q<Button>("reset-button"); resetYes = root.Q<Button>("reset-yes"); resetNo = root.Q<Button>("reset-no");
            pauseClose = root.Q<Button>("pause-close"); restartButton = root.Q<Button>("restart-button"); menuButton = root.Q<Button>("menu-button");
            confirmYes = root.Q<Button>("confirm-yes"); confirmNo = root.Q<Button>("confirm-no");
            levelNumber = root.Q<Label>("level-number"); timerText = root.Q<Label>("timer-text");
            livesCount = root.Q<Label>("lives-count"); lifeCountdown = root.Q<Label>("life-countdown"); coinCount = root.Q<Label>("coin-count");
            readyPrompt = root.Q<Label>("ready-prompt"); confirmText = root.Q<Label>("confirm-text"); soundToggle = root.Q<Toggle>("sound-toggle");
            CacheResults();
            return true;
        }

        private void BindButtons(bool bind)
        {
            if (bind)
            {
                startButton.clicked += PrepareLevel; startSettings.clicked += OpenSettings; settingsClose.clicked += CloseSettings;
                resetButton.clicked += AskReset; resetYes.clicked += ResetProgress; resetNo.clicked += CancelReset;
                pauseSettings.clicked += OpenPause; pauseClose.clicked += ClosePause;
                restartButton.clicked += AskRestart; menuButton.clicked += AskMainMenu;
                confirmYes.clicked += ConfirmPause; confirmNo.clicked += CancelPause;
                soundToggle.RegisterValueChangedCallback(SetSound);
            }
            else
            {
                startButton.clicked -= PrepareLevel; startSettings.clicked -= OpenSettings; settingsClose.clicked -= CloseSettings;
                resetButton.clicked -= AskReset; resetYes.clicked -= ResetProgress; resetNo.clicked -= CancelReset;
                pauseSettings.clicked -= OpenPause; pauseClose.clicked -= ClosePause;
                restartButton.clicked -= AskRestart; menuButton.clicked -= AskMainMenu;
                confirmYes.clicked -= ConfirmPause; confirmNo.clicked -= CancelPause;
                soundToggle.UnregisterValueChangedCallback(SetSound);
            }
        }

        private void OnDisable()
        {
            if (waitingForTree) { waitingForTree = false; return; }
            if (root == null) return;
            root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            root.UnregisterCallback<ClickEvent>(OnButtonSound, TrickleDown.TrickleDown);
            BindResults(false);
            StopResults();
            if (layoutMode != null) layoutMode.Changed -= ApplyLayout;
            BindButtons(false);
            if (subscribed)
            {
                gameManager.StateChanged -= DisplayState; gameManager.LevelLoaded -= ShowLevel;
                levelTimer.TimeChanged -= ShowTime; livesSystem.Changed -= ShowLives; SaveSystem.CoinsChanged -= ShowCoins;
                gameManager.PlayBlockedNoLives -= ShowNoLives;
                subscribed = false;
            }
            CloseSettings(); ClosePause();
            startPulse?.Rewind(); readyPulse?.Rewind(); urgentPulse?.Rewind(); coinPulse?.Rewind();
        }

        private void OnDestroy() => KillTweens();

        private void BuildTweens()
        {
            if (startPulse != null) return;
            startPulse = Reusable(DOTween.To(GetStartScale, SetStartScale, 1.04f, 1.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
            readyPulse = Reusable(DOTween.To(GetReadyPulse, SetReadyPulse, .65f, .85f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo));
            urgentPulse = Reusable(DOTween.To(GetTimerScale, SetTimerScale, 1.16f, .15f).SetLoops(2, LoopType.Yoyo));
            coinPulse = Reusable(DOTween.To(GetCoinScale, SetCoinScale, 1.12f, .09f).SetLoops(2, LoopType.Yoyo));
        }

        private static Tween Reusable(Tween tween) => tween.SetUpdate(true).SetAutoKill(false).Pause();
        private float GetStartScale() => 1f;
        private float GetReadyPulse() => 1f;
        private float GetTimerScale() => 1f;
        private float GetCoinScale() => 1f;
        private void SetStartScale(float value) => ScaleElement(startButton, value);
        private void SetReadyPulse(float value)
        {
            readyPrompt.style.opacity = value;
            ScaleElement(readyPrompt, 1f + (1f - value) * (.04f / .35f));
        }
        private void SetTimerScale(float value) => ScaleElement(timerText, value);
        private void SetCoinScale(float value) => ScaleElement(coinCount, value);
        private void KillTweens()
        {
            startPulse?.Kill(); readyPulse?.Kill(); urgentPulse?.Kill(); coinPulse?.Kill();
            startPulse = readyPulse = urgentPulse = coinPulse = null;
            KillResultTweens();
        }

        private static void Show(VisualElement element, bool visible) => element.EnableInClassList("hidden", !visible);

        // Also usable by Edit mode review without changing GameManager state or saved progress.
        public void DisplayState(GameState state)
        {
            if (shownState == GameState.Complete && state != GameState.Complete) FinishCelebration();
            shownState = state;
            CloseSettings(); ClosePause();
            bool isStart = state == GameState.Start;
            bool isReady = state == GameState.Ready;
            Show(background, isStart); Show(start, isStart);
            Show(hud, isReady || state == GameState.Playing);
            Show(ready, isReady); Show(readyDim, isReady);
            Show(pauseSettings, true);
            pauseSettings.style.visibility = state == GameState.Playing ? Visibility.Visible : Visibility.Hidden;
            startPulse?.Rewind(); readyPulse?.Rewind();
            if (isStart) startPulse?.Restart();
            if (isReady) readyPulse?.Restart();
            if (state != GameState.Playing) urgentPulse?.Rewind();
            DisplayResults(state);
        }

        private void ShowLevel(int number, LevelData level)
        {
            textBuffer.Clear().Append("Level ").Append(number);
            startButton.text = textBuffer.ToString();
            startButton.style.fontSize = Mathf.Min(76f, 720f / textBuffer.Length);
            textBuffer.Clear().Append(number);
            levelNumber.text = textBuffer.ToString();
            levelNumber.style.fontSize = Mathf.Min(70f, 260f / textBuffer.Length);
        }

        private void ShowTime(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            if (seconds == lastTime) return;
            lastTime = seconds;
            timerText.text = Clock(seconds, false);
            timerText.EnableInClassList("urgent", seconds < 10);
            urgentPulse?.Rewind();
            if (shownState == GameState.Playing && seconds > 0 && seconds < 10) urgentPulse?.Restart();
        }

        private string Clock(int seconds, bool twoMinuteDigits)
        {
            textBuffer.Clear();
            int minutes = seconds / 60;
            if (twoMinuteDigits && minutes < 10) textBuffer.Append('0');
            textBuffer.Append(minutes).Append(':').Append((char)('0' + seconds % 60 / 10)).Append((char)('0' + seconds % 10));
            return textBuffer.ToString();
        }

        private void ShowLives(int count, int seconds)
        {
            if (count > 0) HideNoLives();
            if (count != lastLives)
            {
                textBuffer.Clear().Append(count); livesCount.text = textBuffer.ToString();
            }
            if (count != lastLives || seconds != lastLifeSeconds)
                lifeCountdown.text = count >= livesSystem.MaxLives ? "FULL" : Clock(seconds, true);
            lastLives = count; lastLifeSeconds = seconds;
            ShowNoLivesTime(count, seconds);
        }

        private void ShowCoins(int count)
        {
            ShowResultCoins(count);
            if (count == lastCoins) return;
            textBuffer.Clear().Append(count); coinCount.text = textBuffer.ToString();
            coinCount.style.fontSize = Mathf.Min(48f, 250f / textBuffer.Length);
            if (lastCoins >= 0) { coinPulse?.Rewind(); coinPulse?.Restart(); }
            lastCoins = count;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => ApplyLayout(layoutMode.Mode);
        private void ApplyLayout(LayoutMode mode)
        {
            root.EnableInClassList("portrait", mode == LayoutMode.Portrait);
            root.EnableInClassList("square", mode == LayoutMode.Square);
            root.EnableInClassList("landscape", mode == LayoutMode.Landscape);
            Rect safe = layoutMode.SafeViewport;
            float width = root.resolvedStyle.width, height = root.resolvedStyle.height;
            if (float.IsNaN(width) || float.IsNaN(height)) return;
            root.style.paddingLeft = width * safe.xMin;
            root.style.paddingRight = width * (1f - safe.xMax);
            root.style.paddingTop = height * (1f - safe.yMax);
            root.style.paddingBottom = height * safe.yMin;
        }

        private void PrepareLevel() { if (Application.isPlaying) gameManager.PrepareLevel(); }
        private void OnButtonSound(ClickEvent evt)
        {
            for (VisualElement element = evt.target as VisualElement; element != null && element != root; element = element.parent)
                if (element is Button) { if (Application.isPlaying) AudioManager.Play(Sfx.Button); return; }
        }
        private void OpenSettings() { if (shownState != GameState.Start) return; CancelReset(); Show(settings, true); }
        private void CloseSettings() => Show(settings, false);
        private void AskReset() { Show(resetButton, false); Show(resetConfirmation, true); }
        private void CancelReset() { Show(resetButton, true); Show(resetConfirmation, false); }
        private void ResetProgress()
        {
            if (!Application.isPlaying || shownState != GameState.Start || resetConfirmation.ClassListContains("hidden")) return;
            CloseSettings(); gameManager.ResetProgress();
        }
        private void OpenPause()
        {
            if (shownState != GameState.Playing) return;
            if (Application.isPlaying) gameManager.SetPaused(true);
            soundToggle.SetValueWithoutNotify(!AudioManager.Muted);
            RefreshSoundLabel(); CancelPause(); Show(pause, true);
        }
        private void ClosePause()
        {
            Show(pause, false); CancelPause();
            if (Application.isPlaying && gameManager != null && gameManager.IsPaused) gameManager.SetPaused(false);
        }
        private void SetSound(ChangeEvent<bool> evt)
        {
            if (Application.isPlaying) AudioManager.Muted = !evt.newValue;
            RefreshSoundLabel();
        }
        private void RefreshSoundLabel() => soundToggle.label = AudioManager.Muted ? "Sound: OFF" : "Sound: ON";
        private void AskRestart() => Ask(PendingAction.Restart, "Restart this level?\nYou will lose a life.");
        private void AskMainMenu() => Ask(PendingAction.MainMenu, "Return to the main menu?\nYou will lose a life.");
        private void Ask(PendingAction action, string question)
        {
            pending = action; confirmText.text = question; Show(pauseConfirmation, true);
        }
        private void CancelPause() { pending = PendingAction.None; Show(pauseConfirmation, false); }
        private void ConfirmPause()
        {
            if (!Application.isPlaying || pending == PendingAction.None || shownState != GameState.Playing) return;
            PendingAction action = pending;
            ClosePause();
            if (action == PendingAction.Restart) gameManager.Restart();
            else gameManager.QuitToMenu();
        }
    }
}
