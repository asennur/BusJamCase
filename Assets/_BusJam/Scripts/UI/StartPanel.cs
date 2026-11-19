using BusJam.Core;
using BusJam.Level;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>Starts the selected level with an unscaled idle button pulse.</summary>
    public class StartPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text levelText;
        private Tween pulse;

        private void Awake() => startButton.onClick.AddListener(gameManager.StartPlaying);
        private void OnDestroy()
        {
            startButton.onClick.RemoveListener(gameManager.StartPlaying);
            pulse?.Kill();
        }

        private void OnEnable()
        {
            gameManager.LevelLoaded += OnLevelLoaded;
            OnLevelLoaded(gameManager.CurrentLevelNumber, gameManager.CurrentLevel);
            if (pulse == null)
                pulse = startButton.transform.DOScale(1.04f, 1.1f).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetAutoKill(false);
            pulse.Restart();
        }

        private void OnDisable()
        {
            gameManager.LevelLoaded -= OnLevelLoaded;
            pulse?.Pause();
            startButton.transform.localScale = Vector3.one;
        }

        private void OnLevelLoaded(int number, LevelData level) => levelText.SetText("Level {0}", number);
    }
}
