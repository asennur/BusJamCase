using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Save;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Shows each introduction until its first accepted passenger tap.</summary>
    public sealed class TapTutorial : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameplayController gameplay;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform overlay;
        [SerializeField] private RectTransform handAnchor;
        [SerializeField] private RectTransform hand;
        [SerializeField] private GameObject tip;
        [SerializeField] private TMP_Text hint;
        private Passenger target;
        private Tween tapping;
        private int introduction;

        private void Awake()
        {
            tapping = DOTween.Sequence()
                .Append(hand.DOAnchorPosY(18, .4f).SetEase(Ease.InOutSine))
                .Append(hand.DOAnchorPosY(0, .18f).SetEase(Ease.InQuad))
                .Join(hand.DOScale(.9f, .18f))
                .Append(hand.DOScale(1f, .18f))
                .SetLoops(-1).SetUpdate(true).SetAutoKill(false).Pause();
            Hide();
        }
        private void OnEnable()
        {
            gameManager.PlayingStarted += Show;
            gameManager.StateChanged += OnState;
            gameplay.TapAccepted += Accepted;
        }
        private void OnDisable()
        {
            gameManager.PlayingStarted -= Show;
            gameManager.StateChanged -= OnState;
            gameplay.TapAccepted -= Accepted;
            Hide();
        }
        private void OnDestroy() => tapping?.Kill();
        private void OnState(GameState state)
        {
            if (state != GameState.Playing) Hide();
        }
        private void Show()
        {
            int level = gameManager.CurrentLevelNumber;
            if ((level != 1 && level != 6 && level != 8) || SaveSystem.TutorialSeen(level)) return;
            target = gameplay.FirstTappablePassenger();
            if (target == null) return;
            introduction = level;
            hint.text = level == 6 ? "Free tied passengers by moving their neighbours."
                : level == 8 ? "Cloaked passengers can only move when their colour shows." : "";
            tip.SetActive(level != 1);
            handAnchor.gameObject.SetActive(true);
            tapping.Restart();
            PositionHand();
        }
        private void Accepted(Passenger passenger)
        {
            if (introduction == 0) return;
            SaveSystem.CompleteTutorial(introduction);
            Hide();
        }
        private void Hide()
        {
            introduction = 0;
            target = null;
            tapping?.Pause();
            handAnchor.gameObject.SetActive(false);
            tip.SetActive(false);
        }
        private void LateUpdate()
        {
            if (target != null) PositionHand();
        }
        private void PositionHand()
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(target.transform.position + Vector3.up * 1.6f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out Vector2 local);
            handAnchor.anchoredPosition = local + new Vector2(0, 85);
        }
    }
}
