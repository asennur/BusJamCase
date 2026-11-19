using BusJam.Core;
using BusJam.Gameplay;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>Shows the level badge, countdown and life-consuming restart.</summary>
    public class HudPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LevelTimer levelTimer;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Button restartButton;
        private readonly char[] timeBuffer = new char[12];
        private Tween punch;

        private void Awake() => restartButton.onClick.AddListener(gameManager.Restart);
        private void OnDestroy()
        {
            restartButton.onClick.RemoveListener(gameManager.Restart);
            punch?.Kill();
        }

        private void OnEnable()
        {
            levelText.SetText("{0}", gameManager.CurrentLevelNumber);
            OnTimeChanged(levelTimer.SecondsRemaining);
            levelTimer.TimeChanged += OnTimeChanged;
        }

        private void OnDisable()
        {
            levelTimer.TimeChanged -= OnTimeChanged;
            punch?.Kill();
            timeText.transform.localScale = Vector3.one;
        }

        private void OnTimeChanged(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            int minutes = seconds / 60;
            int digits = 1;
            for (int n = minutes; n >= 10; n /= 10) digits++;
            int offset = digits;
            for (int i = digits - 1; i >= 0; i--)
            {
                timeBuffer[i] = (char)('0' + minutes % 10);
                minutes /= 10;
            }
            timeBuffer[offset++] = ':';
            timeBuffer[offset++] = (char)('0' + seconds % 60 / 10);
            timeBuffer[offset++] = (char)('0' + seconds % 10);
            timeText.SetCharArray(timeBuffer, 0, offset);
            bool urgent = seconds > 0 && seconds < 10;
            timeText.color = seconds < 10 ? new Color(1f, 0.16f, 0.18f) : Color.white;
            punch?.Kill();
            timeText.transform.localScale = Vector3.one;
            if (urgent)
                punch = timeText.transform.DOPunchScale(Vector3.one * 0.16f, 0.3f, 1, 0.4f);
        }
    }
}
