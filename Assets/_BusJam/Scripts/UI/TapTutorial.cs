using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.Save;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    /// <summary>Normal hamle ve kaybetme kurallarını değiştirmeden giriş dokunuşlarını yönlendirir.</summary>
    public sealed partial class TapTutorial : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameplayController gameplay;
        [SerializeField] private BusStation busStation;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private UIDocument document;
        private Passenger target;
        private int introduction;
        private int steps;
        private bool guided;
        private Passenger tiedPassenger;
        private Bus firstBus;
        private Vector2Int tunnelMouth;
        private bool tunnelLessonActive;
        private Passenger hiddenPassenger;

        // Gizli yolcular ilk kez Level_02'de görünür.
        private const int HiddenIntroduction = 2;
        private const float HeadTipHeight = 1.5f;

        // LevelDatabase tünelleri ilk kez Level_03'te tanıtır.
        private const int TunnelIntroduction = 3;
        // Level_03 tüneli Green, Yellow, Yellow, Green sırasıyla çıkarır.
        private const PassengerColor TunnelColor = PassengerColor.Green;

        private void Awake()
        {
            CacheHand();
            Hide();
        }
        private void OnEnable()
        {
            if (document == null || gameManager == null || gameplay == null || busStation == null) return;
            if (!CacheHand()) { waitingForTree = true; return; }
            waitingForTree = false;
            if (bound) return;
            bound = true;
            gameManager.PlayingStarted += Show;
            gameManager.StateChanged += OnState;
            gameplay.TapAccepted += Accepted;
            busStation.BusFilled += OnBusFilled;
            busStation.BusArrived += OnBusArrived;
        }
        private void OnDisable()
        {
            waitingForTree = false;
            if (!bound) return;
            bound = false;
            gameManager.PlayingStarted -= Show;
            gameManager.StateChanged -= OnState;
            gameplay.TapAccepted -= Accepted;
            busStation.BusFilled -= OnBusFilled;
            busStation.BusArrived -= OnBusArrived;
            Hide();
        }
        private void OnDestroy() { tapping?.Kill(); handTravel?.Kill(); }
        private void OnState(GameState state)
        {
            if (state != GameState.Playing) Hide();
        }
        private void Show()
        {
            Hide();
            int level = gameManager.CurrentLevelNumber;
            if ((level != 1 && level != HiddenIntroduction && level != TunnelIntroduction && level != 6 && level != 8)
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
            else if (level == HiddenIntroduction)
            {
                target = FindHiddenLesson();
                guided = target != null;
            }
            else if (level == TunnelIntroduction)
            {
                LevelData data = gameManager.CurrentLevel;
                if (data.tunnels.Count == 0) { Hide(); return; }
                tunnelMouth = data.tunnels[0].MouthCell;
                // Otobüs Ready sırasında, PlayingStarted dersi kurmadan önce gelebilir.
                OnBusArrived(busStation.ActiveBus);
                return;
            }
            else target = gameplay.FirstTappablePassenger();

            // Desteklenmeyen düzenler serbest oyunu açık tutar ve tamamlanmış ders kaydetmez.
            if (target == null) { Hide(); return; }
            if (guided) gameplay.TapFilter = AllowsTap;
            hint.text = level == HiddenIntroduction ? "Hidden passengers show their colour once a path opens."
                : level == 6 ? "Free tied passengers by moving their neighbours."
                : level == 8 ? "Cloaked passengers can only move when their colour shows." : "";
            ShowTip(level != 1);
            PointAt(target);
        }

        private bool AllowsTap(Passenger passenger)
        {
            // LateUpdate'in yanında girdi sırasında da yeniden kontrol edilir: eskimiş bir hedef asla dokunuşu engellememeli.
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
            firstBus = bus;
            hint.text = "Tap the passenger at the tunnel; the next one steps out behind.";
            RefreshTunnelTarget();
            if (introduction != 0) ShowTip(true);
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
                HideHand();
                return;
            }
            PositionHand();
            TravelToTarget();
        }

        // İki komşu da başta geçerli olmalı; uyumsuz bir level asla kilitlenmez.
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

        // Yolu açılınca rengi görünecek gizli yolcuyu ve onun dokunulabilir komşusunu seçer; otobüs rengi önceliklidir.
        private Passenger FindHiddenLesson()
        {
            Board board = gameManager.Board;
            Bus bus = busStation.ActiveBus;
            Passenger fallback = null;
            for (int i = 0; i < board.CellCount; i++)
            {
                Passenger hidden = board.GetOccupant(i);
                if (hidden == null || !hidden.IsHidden) continue;
                for (int j = 0; j < board.CellCount; j++)
                {
                    Passenger neighbor = board.GetOccupant(j);
                    if (!IsNeighbor(hidden, neighbor) || neighbor.IsHidden || !gameplay.CanTapPassenger(neighbor)) continue;
                    if (bus != null && neighbor.Color == bus.Color) { hiddenPassenger = hidden; return neighbor; }
                    if (fallback == null) { hiddenPassenger = hidden; fallback = neighbor; }
                }
            }
            return fallback;
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
            // Güvenlik girdiyi serbest bırakır ama ip ipucu yine son ipi bekler.
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
                if (introduction == HiddenIntroduction)
                {
                    // Reveal TapAccepted'dan önce çalışır; açılan yolcu dokunulabilirse el ona geçer.
                    if (passenger != hiddenPassenger && !hiddenPassenger.IsHidden && gameplay.CanTapPassenger(hiddenPassenger))
                        PointAt(hiddenPassenger);
                    else Complete();
                    return;
                }
                if (introduction == TunnelIntroduction)
                {
                    // TapAccepted, kuyruktaki sıradaki yolcu çıktıktan sonra çalışır.
                    Passenger mouthPassenger = gameManager.Board.GetOccupant(tunnelMouth);
                    if (passenger.Color == TunnelColor
                        && !gameplay.TunnelContainsColor(tunnelMouth, TunnelColor)
                        && (mouthPassenger == null || mouthPassenger.Color != TunnelColor)) Complete();
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
            else if (introduction == TunnelIntroduction && tunnelLessonActive && bus == firstBus) Complete();
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
            hiddenPassenger = null;
            target = null;
            HideHand();
            ShowTip(false);
        }
        private void LateUpdate()
        {
            if (guided && !gameplay.CanTapPassenger(target)) ReleaseGuidance();
            if (target != null) PositionHand();
        }
        private void PositionHand()
        {
            // Parmak ucu başın hemen üstünde: model ölçek 1'de 1,41 boyunda, board ölçeğiyle büyür.
            Vector3 head = target.transform.position + Vector3.up * (HeadTipHeight * target.transform.lossyScale.y);
            Vector3 screen = worldCamera.WorldToScreenPoint(head);
            desiredPosition = overlay.WorldToLocal(RuntimePanelUtils.ScreenToPanel(overlay.panel, new Vector2(screen.x, Screen.height - screen.y)));
            if (handVisible && !travelling && (desiredPosition - handPosition).sqrMagnitude > 4) TravelToTarget();
        }
    }
}
