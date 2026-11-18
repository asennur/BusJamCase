using BusJam.Core;
using TMPro;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Start ekranı: "Tap to start" sabit metni + levelin süre sınırı.</summary>
    public class StartPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TMP_Text timeText;

        // Panel UIManager tarafından aktif edildiğinde level zaten yüklenmiş olur; event beklemeye gerek yok.
        private void OnEnable() => timeText.text = $"{(int)gameManager.CurrentLevel.timeLimit}s";
    }
}
