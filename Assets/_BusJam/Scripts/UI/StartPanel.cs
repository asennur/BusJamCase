using BusJam.Core;
using BusJam.Level;
using TMPro;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Start ekranı: "Tap to start" sabit metni + levelin süre sınırı.</summary>
    public class StartPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text timeText;

        private void OnEnable()
        {
            if (gameManager == null || timeText == null)
            {
                Debug.LogError("StartPanel için Game Manager ve Time Text alanlarını bağla.", this);
                return;
            }

            gameManager.LevelLoaded += OnLevelLoaded;
            if (gameManager.CurrentLevel != null)
                OnLevelLoaded(gameManager.CurrentLevelNumber, gameManager.CurrentLevel);
        }

        private void OnDisable()
        {
            if (gameManager != null)
                gameManager.LevelLoaded -= OnLevelLoaded;
        }

        private void OnLevelLoaded(int _, LevelData level) =>
            timeText.text = $"{Mathf.CeilToInt(level.timeLimit)}s";
    }
}
