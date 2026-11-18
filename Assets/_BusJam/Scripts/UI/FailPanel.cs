using BusJam.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>Fail paneli: kaybetme sebebini gösterir, Retry butonu leveli yeniden kurar.</summary>
    public class FailPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private Button retryButton;

        private void Awake() => retryButton.onClick.AddListener(gameManager.Retry);

        private void OnEnable() => reasonText.text = ToText(gameManager.FailReason);

        private static string ToText(FailReason reason) => reason switch
        {
            FailReason.OutOfTime => "Out of time",
            _ => "No space"
        };
    }
}
