using System.Collections;
using BusJam.Core;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Selects the current panel and shows a temporary warning when play needs lives.</summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LivesSystem livesSystem;
        [SerializeField] private GameObject coinCounter;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private GameObject failPanel;
        [SerializeField] private GameObject noLivesPanel;
        [SerializeField] private GameObject readyPanel;
        [SerializeField] private bool toolkitScreens;
        [SerializeField, Min(0f)] private float noLivesDuration = 3.5f;

        private Coroutine noLivesWarning;
        private float hideAt;

        private void OnEnable()
        {
            gameManager.StateChanged += OnStateChanged;
            gameManager.PlayBlockedNoLives += ShowNoLives;
            livesSystem.Changed += OnLivesChanged;
        }

        private void OnDisable()
        {
            gameManager.StateChanged -= OnStateChanged;
            gameManager.PlayBlockedNoLives -= ShowNoLives;
            livesSystem.Changed -= OnLivesChanged;
            HideNoLives();
        }

        private void OnLivesChanged(int lives, int seconds)
        {
            if (lives > 0) HideNoLives();
        }

        private void ShowNoLives()
        {
            if (gameManager.State != GameState.Start) return;
            hideAt = Time.unscaledTime + noLivesDuration;
            noLivesPanel.SetActive(true);
            if (noLivesWarning == null) noLivesWarning = StartCoroutine(HideNoLivesWhenDue());
        }

        // Repeated presses only extend the deadline; the running coroutine is reused.
        private IEnumerator HideNoLivesWhenDue()
        {
            do { yield return null; }
            while (Time.unscaledTime < hideAt);
            noLivesWarning = null;
            noLivesPanel.SetActive(false);
        }

        private void HideNoLives()
        {
            if (noLivesWarning != null) StopCoroutine(noLivesWarning);
            noLivesWarning = null;
            noLivesPanel.SetActive(false);
        }

        private void OnStateChanged(GameState state)
        {
            bool start = state == GameState.Start;
            if (coinCounter != null) coinCounter.SetActive(state == GameState.Complete);
            startPanel.SetActive(start && !toolkitScreens);
            HideNoLives();
            hudPanel.SetActive(!toolkitScreens && (state == GameState.Ready || state == GameState.Playing));
            readyPanel.SetActive(!toolkitScreens && state == GameState.Ready);
            completePanel.SetActive(state == GameState.Complete);
            failPanel.SetActive(state == GameState.Fail);
        }
    }
}
