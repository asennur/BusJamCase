using BusJam.Core;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Selects the current panel and gates the start screen on available lives.</summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LivesSystem livesSystem;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private GameObject failPanel;
        [SerializeField] private GameObject noLivesPanel;

        private void OnEnable()
        {
            gameManager.StateChanged += OnStateChanged;
            livesSystem.Changed += OnLivesChanged;
        }

        private void OnDisable()
        {
            gameManager.StateChanged -= OnStateChanged;
            livesSystem.Changed -= OnLivesChanged;
        }

        private void OnLivesChanged(int lives, int seconds)
        {
            if (gameManager.State == GameState.Start) OnStateChanged(GameState.Start);
        }

        private void OnStateChanged(GameState state)
        {
            bool start = state == GameState.Start;
            startPanel.SetActive(start && livesSystem.Lives > 0);
            noLivesPanel.SetActive(start && livesSystem.Lives == 0);
            hudPanel.SetActive(state == GameState.Playing);
            completePanel.SetActive(state == GameState.Complete);
            failPanel.SetActive(state == GameState.Fail);
        }
    }
}
