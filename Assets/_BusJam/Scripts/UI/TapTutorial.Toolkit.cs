using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    public sealed partial class TapTutorial
    {
        private VisualElement tutorialDocumentRoot, overlay, pointer, hand, ring, tip;
        private Label hint;
        private readonly VisualElement[] tapDots = new VisualElement[8];
        private Tween tapping, handTravel;
        private Vector2 desiredPosition, handPosition, travelStart;
        private bool handVisible, travelling, farTravel;

        private void CacheHand()
        {
            if (document == null || tutorialDocumentRoot == document.rootVisualElement) return;
            tapping?.Kill(); handTravel?.Kill();
            tutorialDocumentRoot = document.rootVisualElement;
            overlay = tutorialDocumentRoot.Q("tutorial-overlay"); pointer = overlay.Q("tutorial-pointer");
            hand = overlay.Q("tutorial-hand"); ring = overlay.Q("tutorial-ring");
            tip = overlay.Q("tutorial-tip"); hint = overlay.Q<Label>("tutorial-hint");
            var burst = overlay.Q("tutorial-burst");
            for (int i = 0; i < tapDots.Length; i++)
            {
                tapDots[i] = new VisualElement { pickingMode = PickingMode.Ignore };
                tapDots[i].AddToClassList("pooled-effect"); tapDots[i].AddToClassList("tutorial-dot");
                burst.Add(tapDots[i]);
            }
            tapping = DOTween.To(HandZero, AnimateTap, .9f, .9f).SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true).SetAutoKill(false).Pause();
            handTravel = DOTween.To(HandZero, AnimateTravel, .85f, .85f).SetEase(Ease.Linear).SetUpdate(true).SetAutoKill(false).Pause();
            handTravel.OnComplete(ArriveAtTarget);
            HideHand();
        }

        private static float HandZero() => 0;
        private static void HandScale(VisualElement element, float value) => element.style.scale = new Scale(new Vector3(value, value, 1));
        private void ShowTip(bool visible) { if (tip != null) tip.EnableInClassList("hidden", !visible); }
        private void HideHand()
        {
            tapping?.Pause(); handTravel?.Pause();
            handVisible = travelling = false;
            if (pointer != null) pointer.AddToClassList("hidden");
        }

        private void TravelToTarget()
        {
            if (pointer == null) return;
            tapping.Pause();
            if (!handVisible)
            {
                handVisible = true;
                pointer.RemoveFromClassList("hidden");
                ArriveAtTarget();
                return;
            }
            travelStart = handPosition;
            farTravel = Vector2.Distance(travelStart, desiredPosition) > tutorialDocumentRoot.resolvedStyle.height * .4f;
            handVisible = travelling = true;
            pointer.RemoveFromClassList("hidden");
            AnimateTravel(0); handTravel.Restart();
        }

        // Press at the old fingertip before gliding; distant targets crossfade while invisible.
        private void AnimateTravel(float time)
        {
            float p = Mathf.Clamp01((time - .35f) / .5f);
            AnimateTap(time);
            if (farTravel)
            {
                pointer.style.opacity = p < .5f ? 1 - p * 2 : (p - .5f) * 2;
                handPosition = p < .5f ? travelStart : desiredPosition;
            }
            else
            {
                pointer.style.opacity = 1;
                float smooth = p * p * (3 - 2 * p);
                handPosition = Vector2.Lerp(travelStart, desiredPosition, smooth);
                handPosition.y -= Mathf.Sin(p * Mathf.PI) * Mathf.Min(65, Vector2.Distance(travelStart, desiredPosition) * .15f);
            }
            pointer.style.translate = new Translate(handPosition.x, handPosition.y);
        }

        private void ArriveAtTarget()
        {
            travelling = false; handPosition = desiredPosition;
            pointer.style.opacity = 1;
            pointer.style.translate = new Translate(handPosition.x, handPosition.y);
            tapping.Restart();
        }

        private void AnimateTap(float time)
        {
            float press = time < .12f ? time / .12f : 1 - Mathf.Clamp01((time - .12f) / .18f);
            HandScale(hand, 1 - .15f * press);
            float burst = Mathf.Clamp01((time - .1f) / .35f);
            float opacity = time >= .1f && time < .45f ? 1 - burst : 0;
            ring.style.opacity = opacity;
            HandScale(ring, .4f + burst * 1.1f);
            for (int i = 0; i < tapDots.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / tapDots.Length;
                float radius = 18 + burst * 75;
                tapDots[i].style.translate = new Translate(Mathf.Cos(angle) * radius - 12, Mathf.Sin(angle) * radius - 12);
                tapDots[i].style.opacity = opacity;
                HandScale(tapDots[i], 1 - burst * .7f);
            }
        }
    }
}
