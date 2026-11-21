using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    /// <summary>Full-screen fade used for Play, Next, Retry and pause exits; the level changes while the screen is dark.</summary>
    public sealed partial class ToolkitGameUI
    {
        [Header("Level Transition")]
        [SerializeField, Min(.01f)] private float fadeOutDuration = .3f;
        [SerializeField, Min(.01f)] private float fadeInDuration = .3f;
        [SerializeField, Min(0)] private float fadeHold = .08f;
        [SerializeField] private Color fadeColor = new Color(.03f, .03f, .06f, 1f);

        private enum FadeAction { None, Prepare, Next, Retry, Restart, MainMenu }
        private FadeAction fadeAction;
        private VisualElement fadeOverlay;
        private Tween fadeOut, fadeIn;
        private bool fadeHolding;
        private int fadeResumeFrame;
        private float fadeResumeTime;

        // One overlay above ui-root; hidden (display none) when idle.
        private void CreateFadeOverlay()
        {
            fadeOverlay = new VisualElement { name = "level-fade", pickingMode = PickingMode.Ignore };
            var style = fadeOverlay.style;
            style.position = Position.Absolute;
            style.left = style.top = style.right = style.bottom = 0;
            style.backgroundColor = fadeColor;
            style.opacity = 0;
            style.display = DisplayStyle.None;
            documentRoot.Add(fadeOverlay);
        }

        private void BuildFadeTweens()
        {
            if (fadeOut != null) return;
            fadeOut = Reusable(DOTween.To(GetFadeClear, SetFade, 1f, fadeOutDuration).SetEase(Ease.InOutSine).OnComplete(OnFadedOut));
            fadeIn = Reusable(DOTween.To(GetFadeDark, SetFade, 0f, fadeInDuration).SetEase(Ease.InOutSine).OnComplete(OnFadedIn));
        }

        private static float GetFadeClear() => 0f;
        private static float GetFadeDark() => 1f;
        private void SetFade(float value) => fadeOverlay.style.opacity = value;

        // Ignores new requests until the running transition ends, so double taps cannot skip levels.
        private void BeginTransition(FadeAction action)
        {
            if (fadeAction != FadeAction.None) return;
            fadeAction = action;
            if (fadeOverlay == null || fadeOut == null) { RunFadeAction(); fadeAction = FadeAction.None; return; }
            gameManager.SetInputLocked(true);
            fadeOverlay.style.display = DisplayStyle.Flex;
            fadeOverlay.pickingMode = PickingMode.Position;
            fadeOut.Restart();
        }

        private void OnFadedOut()
        {
            RunFadeAction();
            // The level build can stall a frame; wait so its delta time does not eat the fade-in.
            fadeHolding = true;
            fadeResumeFrame = Time.frameCount + 2;
            fadeResumeTime = Time.unscaledTime + fadeHold;
        }

        private void RunFadeAction()
        {
            switch (fadeAction)
            {
                case FadeAction.Prepare: gameManager.PrepareLevel(); break;
                case FadeAction.Next: gameManager.LoadNext(); break;
                case FadeAction.Retry: gameManager.Retry(); break;
                case FadeAction.Restart: gameManager.Restart(); break;
                case FadeAction.MainMenu: gameManager.QuitToMenu(); break;
            }
        }

        private void UpdateFadeHold()
        {
            if (!fadeHolding || Time.frameCount < fadeResumeFrame || Time.unscaledTime < fadeResumeTime) return;
            fadeHolding = false;
            fadeIn.Restart();
        }

        private void OnFadedIn() => EndTransition();

        private void EndTransition()
        {
            fadeHolding = false;
            if (fadeAction != FadeAction.None && gameManager != null) gameManager.SetInputLocked(false);
            fadeAction = FadeAction.None;
            if (fadeOverlay == null) return;
            fadeOverlay.style.opacity = 0;
            fadeOverlay.style.display = DisplayStyle.None;
            fadeOverlay.pickingMode = PickingMode.Ignore;
        }

        private void StopTransition()
        {
            fadeOut?.Pause(); fadeIn?.Pause();
            EndTransition();
        }

        private void KillFadeTweens()
        {
            fadeOut?.Kill(); fadeIn?.Kill();
            fadeOut = fadeIn = null;
        }
    }
}
