using BusJam.Core;
using BusJam.Gameplay;
using TMPro;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>HUD: sol üstte level numarası, geri sayan süre.</summary>
    public class HudPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LevelTimer levelTimer;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text timeText;

        // Panel aktif olduğunda level zaten yüklenmiştir; yalnız geri sayım için event'e abone olunur.
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
