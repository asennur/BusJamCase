using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Pulses the ready prompt while the level waits for its first tap.</summary>
    public sealed class ReadyPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text prompt;
        private Sequence pulse;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (pulse == null)
            {
                pulse = DOTween.Sequence()
                    .Append(prompt.DOFade(.65f, .85f).SetEase(Ease.InOutSine))
                    .Join(prompt.transform.DOScale(1.04f, .85f).SetEase(Ease.InOutSine))
                    .SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetAutoKill(false).Pause();
            }
            pulse.Restart();
        }

        private void OnDisable()
        {
            pulse?.Rewind();
            prompt.alpha = 1;
            prompt.transform.localScale = Vector3.one;
        }

        private void OnDestroy() => pulse?.Kill();
    }
}
