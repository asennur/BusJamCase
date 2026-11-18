using BusJam.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>Complete paneli: Next butonu sıradaki leveli yükler.</summary>
    public class CompletePanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button nextButton;

        private void Awake() => nextButton.onClick.AddListener(gameManager.LoadNext);
    }
}
