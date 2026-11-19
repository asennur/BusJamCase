using BusJam.Save;
using TMPro;
using DG.Tweening;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Displays saved coins and the pending arrival count.</summary>
    public sealed class CoinCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private float punchDuration = .18f;
        private Tween punch;
        private void Awake() => punch = transform.DOPunchScale(Vector3.one * .12f, punchDuration, 2).SetUpdate(true).SetAutoKill(false).Pause();
        private void OnEnable()
        {
            SaveSystem.ProgressReset += Refresh;
            Refresh();
        }
        private void Refresh() => Show(SaveSystem.Coins);
        public void Show(int value) => countText.SetText("{0}", value);
        public void Arrive(int value)
        {
            Show(value);
            punch?.Rewind();
            transform.localScale = Vector3.one;
            punch?.Restart();
        }
        private void OnDestroy() => punch?.Kill();
        private void OnDisable()
        {
            SaveSystem.ProgressReset -= Refresh;
            punch?.Rewind();
            transform.localScale = Vector3.one;
        }
    }
}
