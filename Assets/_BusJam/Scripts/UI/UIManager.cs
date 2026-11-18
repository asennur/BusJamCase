using BusJam.Core;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>GameManager.StateChanged'i dinler, yalnız ilgili paneli açar. Paneller birbirini bilmez.</summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private GameObject failPanel;

        private void OnEnable() => gameManager.StateChanged += OnStateChanged;
        private void OnDisable() => gameManager.StateChanged -= OnStateChanged;

        private void OnStateChanged(GameState state)
        {
            startPanel.SetActive(state == GameState.Start);
            hudPanel.SetActive(state == GameState.Playing);
            completePanel.SetActive(state == GameState.Complete);
            failPanel.SetActive(state == GameState.Fail);
        }
    }
}
