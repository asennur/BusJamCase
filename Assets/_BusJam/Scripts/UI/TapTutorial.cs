using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.Save;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Guides introductory taps without changing normal move or failure rules.</summary>
    public sealed class TapTutorial : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameplayController gameplay;
        [SerializeField] private BusStation busStation;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform overlay;
        [SerializeField] private RectTransform handAnchor;
        [SerializeField] private RectTransform hand;
        [SerializeField] private GameObject tip;
        [SerializeField] private TMP_Text hint;
        private Passenger target;
        private Tween tapping;
        private int introduction;
        private int steps;
        private bool guided;
        private Passenger tiedPassenger;
        private Bus firstBus;
        private Vector2Int tunnelMouth;
        private bool tunnelLessonActive;

        // LevelDatabase first introduces tunnels in Level_03.
        private const int TunnelIntroduction = 3;
        // Level_03 starts with Green at the mouth and Yellow, Yellow, Green inside.
        private const PassengerColor TunnelColor = PassengerColor.Green;

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
            busStation.BusFilled += OnBusFilled;
            busStation.BusArrived += OnBusArrived;
        }
        private void OnDisable()
        {
            gameManager.PlayingStarted -= Show;
            gameManager.StateChanged -= OnState;
            gameplay.TapAccepted -= Accepted;
            busStation.BusFilled -= OnBusFilled;
            busStation.BusArrived -= OnBusArrived;
            Hide();
        }
        private void OnDestroy() => tapping?.Kill();
        private void OnState(GameState state)
        {
            if (state != GameState.Playing) Hide();
        }
        private void Show()
        {
            Hide();
            int level = gameManager.CurrentLevelNumber;
            if ((level != 1 && level != TunnelIntroduction && level != 6 && level != 8)
                || SaveSystem.TutorialSeen(level)) return;
            introduction = level;
            if (level == 1)
            {
                firstBus = busStation.ActiveBus;
                guided = true;
                target = gameplay.FirstTappablePassenger(gameManager.CurrentLevel.busOrder[0]);
            }
            else if (level == 6)
            {
                tiedPassenger = FindRopeLesson();
                guided = tiedPassenger != null;
                target = guided ? FindTappableNeighbor(tiedPassenger) : gameplay.FirstTappablePassenger();
            }
            else if (level == TunnelIntroduction)
            {
                LevelData data = gameManager.CurrentLevel;
                if (data.tunnels.Count == 0) { Hide(); return; }
                tunnelMouth = data.tunnels[0].MouthCell;
                // A bus can arrive during Ready, before PlayingStarted arms the lesson.
                OnBusArrived(busStation.ActiveBus);
                return;
            }
            else target = gameplay.FirstTappablePassenger();

            // Unsupported layouts keep free play available and do not save a completed lesson.
            if (target == null) { Hide(); return; }
            if (guided) gameplay.TapFilter = AllowsTap;
            hint.text = level == 6 ? "Free tied passengers by moving their neighbours."
                : level == 8 ? "Cloaked passengers can only move when their colour shows." : "";
            tip.SetActive(level != 1);
            PointAt(target);
        }

        private bool AllowsTap(Passenger passenger)
        {
            // Recheck during input as well as LateUpdate: a stale target must never block a tap.
            if (!gameplay.CanTapPassenger(target)) ReleaseGuidance();
            return !guided || passenger == target;
        }

        private void ReleaseGuidance()
        {
            gameplay.TapFilter = null;
            guided = false;
            PointAt(null);
        }

        private void OnBusArrived(Bus bus)
        {
            if (introduction != TunnelIntroduction || tunnelLessonActive
                || gameManager.State != GameState.Playing || bus == null
                || bus != busStation.ActiveBus || !bus.IsAtDoor || bus.Color != TunnelColor) return;

            tunnelLessonActive = true;
            hint.text = "Tap the passenger at the tunnel; the next one steps out behind.";
            RefreshTunnelTarget();
            if (introduction != 0) tip.SetActive(true);
        }

        private void RefreshTunnelTarget()
        {
            Passenger mouthPassenger = gameManager.Board.GetOccupant(tunnelMouth);
            bool greenInside = gameplay.TunnelContainsColor(tunnelMouth, TunnelColor);
            if (greenInside || (mouthPassenger != null && mouthPassenger.Color == TunnelColor))
                target = gameplay.CanTapPassenger(mouthPassenger) ? mouthPassenger : null;
            else
            {
                target = gameplay.FirstTappablePassenger(TunnelColor);
                if (target == null) { Complete(); return; }
            }

            if (target == null) ReleaseGuidance();
            else
            {
                guided = true;
                gameplay.TapFilter = AllowsTap;
                PointAt(target);
            }
        }

        private void PointAt(Passenger passenger)
        {
            target = passenger;
            if (target == null)
            {
                tapping.Pause();
                handAnchor.gameObject.SetActive(false);
                return;
            }
            handAnchor.gameObject.SetActive(true);
            tapping.Restart();
            PositionHand();
        }

        // Both neighbours must be legal at the start; never lock an incompatible level.
        private Passenger FindRopeLesson()
        {
            Board board = gameManager.Board;
            for (int i = 0; i < board.CellCount; i++)
            {
                Passenger tied = board.GetOccupant(i);
                if (tied == null || tied.RopesLeft != 2) continue;
                int count = 0;
                for (int j = 0; j < board.CellCount; j++)
                {
                    Passenger neighbor = board.GetOccupant(j);
                    if (IsNeighbor(tied, neighbor) && gameplay.CanTapPassenger(neighbor)) count++;
                }
                if (count >= 2) return tied;
            }
            return null;
        }

        private Passenger FindTappableNeighbor(Passenger tied)
        {
            Board board = gameManager.Board;
            for (int i = 0; i < board.CellCount; i++)
            {
                Passenger neighbor = board.GetOccupant(i);
                if (IsNeighbor(tied, neighbor) && gameplay.CanTapPassenger(neighbor)) return neighbor;
            }
            return null;
        }

        private static bool IsNeighbor(Passenger tied, Passenger passenger)
        {
            if (passenger == null) return false;
            Vector2Int offset = tied.Cell - passenger.Cell;
            return Mathf.Abs(offset.x) + Mathf.Abs(offset.y) == 1;
        }

        private void Accepted(Passenger passenger)
        {
            if (introduction == 0) return;
            if (introduction == TunnelIntroduction && !tunnelLessonActive) return;
            if (introduction == 1 && steps == GameRules.BusCapacity) return;
            if (introduction == 1 && !guided) { Hide(); return; }
            // Safety releases input, but the rope hint still waits for the last rope.
            if (introduction == 6 && !guided && tiedPassenger != null && tiedPassenger.RopesLeft > 0) return;
            if (guided)
            {
                if (passenger != target) return;
                steps++;
                if (introduction == 1)
                {
                    if (steps == GameRules.BusCapacity) ReleaseGuidance();
                    else
                    {
                        Passenger next = gameplay.FirstTappablePassenger(gameManager.CurrentLevel.busOrder[0]);
                        if (next == null) Hide();
                        else PointAt(next);
                    }
                    return;
                }
                if (introduction == TunnelIntroduction)
                {
                    if (passenger.Color == TunnelColor) Complete();
                    else RefreshTunnelTarget();
                    return;
                }
                if (tiedPassenger.RopesLeft > 0)
                {
                    Passenger next = FindTappableNeighbor(tiedPassenger);
                    if (next == null) ReleaseGuidance();
                    else PointAt(next);
                    return;
                }
            }
            Complete();
        }

        private void OnBusFilled(Bus bus)
        {
            if (introduction == 1 && bus == firstBus && steps == GameRules.BusCapacity) Complete();
        }

        private void Complete()
        {
            SaveSystem.CompleteTutorial(introduction);
            Hide();
        }
        private void Hide()
        {
            gameplay.TapFilter = null;
            introduction = 0;
            steps = 0;
            guided = false;
            tiedPassenger = null;
            firstBus = null;
            tunnelLessonActive = false;
            target = null;
            tapping?.Pause();
            handAnchor.gameObject.SetActive(false);
            tip.SetActive(false);
        }
        private void LateUpdate()
        {
            if (guided && !gameplay.CanTapPassenger(target)) ReleaseGuidance();
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
