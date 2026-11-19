using System;
using BusJam.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>
    /// In-game settings (top-right gear while playing): sound on/off, Restart and Main Menu.
    /// The game is paused while the panel is open. Restart and Main Menu cost a life, so they ask for confirmation first.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button openButton;
        [SerializeField] private GameObject panel;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private TMP_Text soundLabel;
        [SerializeField] private Button restartButton, mainMenuButton, closeButton;

        [Header("Confirm")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private Button yesButton, noButton;

        private Action pendingAction;

        private void Awake()
        {
            panel.SetActive(false);
            confirmPanel.SetActive(false);
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            soundToggle.onValueChanged.AddListener(SetSound);
            restartButton.onClick.AddListener(() => Ask("Restart this level?\nYou will lose a life.", gameManager.Restart));
            mainMenuButton.onClick.AddListener(() => Ask("Return to the main menu?\nYou will lose a life.", gameManager.QuitToMenu));
            yesButton.onClick.AddListener(Confirm);
            noButton.onClick.AddListener(() => confirmPanel.SetActive(false));
        }

        // The HUD is also shown in Ready (before Tap to start); the gear only makes sense while playing.
        private void OnEnable()
        {
            gameManager.StateChanged += OnStateChanged;
            OnStateChanged(gameManager.State);
        }

        private void OnStateChanged(GameState state) => openButton.gameObject.SetActive(state == GameState.Playing);

        // Closing the HUD must not leave the game paused.
        private void OnDisable()
        {
            if (gameManager != null) gameManager.StateChanged -= OnStateChanged;
            panel.SetActive(false);
            confirmPanel.SetActive(false);
            if (gameManager != null && gameManager.IsPaused) gameManager.SetPaused(false);
        }

        private void Open()
        {
            gameManager.SetPaused(true);
            soundToggle.SetIsOnWithoutNotify(!AudioManager.Muted);
            RefreshSoundLabel();
            confirmPanel.SetActive(false);
            panel.SetActive(true);
        }

        private void Close()
        {
            panel.SetActive(false);
            confirmPanel.SetActive(false);
            gameManager.SetPaused(false);
        }

        private void SetSound(bool on)
        {
            AudioManager.Muted = !on;
            RefreshSoundLabel();
        }

        private void RefreshSoundLabel()
        {
            if (soundLabel != null) soundLabel.text = AudioManager.Muted ? "Sound: OFF" : "Sound: ON";
        }

        private void Ask(string question, Action action)
        {
            pendingAction = action;
            confirmText.text = question;
            confirmPanel.SetActive(true);
        }

        private void Confirm()
        {
            Action action = pendingAction;
            pendingAction = null;
            panel.SetActive(false);
            confirmPanel.SetActive(false);
            action?.Invoke(); // Restart / QuitToMenu unpause the game themselves.
        }
    }
}
