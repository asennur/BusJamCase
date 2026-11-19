using BusJam.Core;
using BusJam.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>Shows level time and exposes a life-consuming restart while playing.</summary>
    public class HudPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LevelTimer levelTimer;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Button restartButton;

        private void Awake() => restartButton.onClick.AddListener(gameManager.Restart);

        private void OnDestroy() => restartButton.onClick.RemoveListener(gameManager.Restart);

        // The level is already loaded when the panel becomes active.
        private void OnEnable()
        {
            levelText.text = $"Level {gameManager.CurrentLevelNumber}";
            timeText.text = levelTimer.SecondsRemaining.ToString();
            levelTimer.TimeChanged += OnTimeChanged;
        }

        private void OnDisable() => levelTimer.TimeChanged -= OnTimeChanged;

        private void OnTimeChanged(int seconds) => timeText.text = seconds.ToString();
    }
}
