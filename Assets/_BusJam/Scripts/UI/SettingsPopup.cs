using BusJam.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>Provides a two-step progress reset from the Start screen.</summary>
    public sealed class SettingsPopup : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private GameObject popup;
        [SerializeField] private GameObject confirmation;

        private void Awake()
        {
            settingsButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            resetButton.onClick.AddListener(Confirm);
            yesButton.onClick.AddListener(ResetConfirmed);
            noButton.onClick.AddListener(Cancel);
            Close();
        }
        private void OnEnable() => gameManager.StateChanged += StateChanged;
        private void OnDisable()
        {
            gameManager.StateChanged -= StateChanged;
            Close();
        }
        private void OnDestroy()
        {
            settingsButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            resetButton.onClick.RemoveListener(Confirm);
            yesButton.onClick.RemoveListener(ResetConfirmed);
            noButton.onClick.RemoveListener(Cancel);
        }
        private void StateChanged(GameState state)
        {
            if (state != GameState.Start) Close();
        }
        private void Open()
        {
            if (gameManager.State != GameState.Start) return;
            Cancel();
            popup.SetActive(true);
        }
        private void Close() => popup.SetActive(false);
        private void Confirm()
        {
            resetButton.gameObject.SetActive(false);
            confirmation.SetActive(true);
        }
        private void Cancel()
        {
            resetButton.gameObject.SetActive(true);
            confirmation.SetActive(false);
        }
        private void ResetConfirmed()
        {
            if (!confirmation.activeSelf || gameManager.State != GameState.Start) return;
            Close();
            gameManager.ResetProgress();
        }
    }
}
