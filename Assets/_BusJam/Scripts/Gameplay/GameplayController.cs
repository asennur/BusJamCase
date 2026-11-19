using System;
using System.Collections.Generic;
using BusJam.Core;
using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Kuralların tek yeri: dokunma kararı, hedef rezervasyonu, erişilebilirlik, kazan/kaybet.
    /// Hedef sırası: otobüs (kapıda + aynı renk + boş koltuk, slotlar doluyken devre dışı) → bekleme slotu →
    /// slotlar doluysa, bir slot ileride açılacaksa slot sırasının arkası (taşma kuyruğu, Asen kararı 19 Kasım);
    /// hiç açılmayacaksa dokunma yok sayılır.
    /// </summary>
    public class GameplayController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private BoardView boardView;
        [SerializeField] private BusStation busStation;
        [SerializeField] private WaitingArea waitingArea;
        [SerializeField] private float runSpeed = 4.8f;

        private Board board;
        private bool[] reachable;
        private int runningCount;
        private readonly List<Vector2Int> pathBuffer = new List<Vector2Int>();
        private readonly List<Vector3> worldPathBuffer = new List<Vector3>();
        private readonly List<TunnelGroup> tunnelGroups = new List<TunnelGroup>();

        // Slotlar doluyken dokunulan, bir slot açılınca sırayla yerleşecek yolcular (FIFO, index 0 = en önde).
        private readonly List<Passenger> overflowQueue = new List<Passenger>();

        // Çalışma zamanı tünel durumu: level verisindeki kuyruk burada tüketilir, asset değişmez.
        private class TunnelGroup
        {
            public Vector2Int tunnelCell;
            public Vector2Int mouthCell;
            public Queue<PassengerColor> queue;
            public TunnelView view;
        }

        public event Action LevelCompleted;
        public event Action<FailReason> LevelFailed;

        private void OnEnable()
        {
            gameManager.LevelLoaded += OnLevelLoaded;
            inputHandler.PassengerTapped += OnPassengerTapped;
            busStation.BusArrived += BoardWaitingPassengers;
            busStation.BusDeparted += OnBusDeparted;
            busStation.AllBusesDone += OnAllBusesDone;
            waitingArea.SlotOpened += OnSlotOpened;
        }

        private void OnDisable()
        {
            gameManager.LevelLoaded -= OnLevelLoaded;
            inputHandler.PassengerTapped -= OnPassengerTapped;
            busStation.BusArrived -= BoardWaitingPassengers;
            busStation.BusDeparted -= OnBusDeparted;
            busStation.AllBusesDone -= OnAllBusesDone;
            waitingArea.SlotOpened -= OnSlotOpened;
        }

        private void OnLevelLoaded(int levelNumber, LevelData level)
        {
            board = gameManager.Board;
            if (reachable == null || reachable.Length < board.CellCount)
                reachable = new bool[board.CellCount];

            runningCount = 0;
            for (int i = 0; i < overflowQueue.Count; i++)
                boardView.ReleasePassenger(overflowQueue[i]);
            overflowQueue.Clear();

            SetupTunnels(level);
            waitingArea.Setup(levelNumber);
            busStation.Setup(level.busOrder);
            RefreshReachable();
        }

        private void SetupTunnels(LevelData level)
        {
            tunnelGroups.Clear();
            for (int i = 0; i < level.tunnels.Count; i++)
            {
                TunnelData data = level.tunnels[i];
                var group = new TunnelGroup
                {
                    tunnelCell = data.cell,
                    mouthCell = data.MouthCell,
                    queue = new Queue<PassengerColor>(data.queue),
                    view = boardView.GetTunnelView(data.cell)
                };
                tunnelGroups.Add(group);
                EmergeFromTunnel(group);
            }
        }

        // Ağız hücresi boşsa ve kuyrukta yolcu varsa sıradakini çıkarır (level kurulurken de, oyun sırasında da).
        private void EmergeFromTunnel(TunnelGroup group)
        {
            if (group.queue.Count == 0 || board.GetOccupant(group.mouthCell) != null)
                return;

            PassengerColor color = group.queue.Dequeue();
            Passenger passenger = boardView.SpawnFromTunnel(group.tunnelCell, group.mouthCell, color);
            board.SetOccupant(group.mouthCell, passenger);
            group.view?.SetRemaining(group.queue.Count);
        }

        // Dokunulan yolcu hücresini bıraktığı an çağrılır: ağız o hücreyse kuyruktaki sıradaki yolcu çıkar.
        private void TryEmergeFromTunnel(Vector2Int vacatedCell)
        {
            for (int i = 0; i < tunnelGroups.Count; i++)
            {
                if (tunnelGroups[i].mouthCell == vacatedCell)
                {
                    EmergeFromTunnel(tunnelGroups[i]);
                    return;
                }
            }
        }

        private void OnPassengerTapped(Passenger passenger)
        {
            if (gameManager.State != GameState.Playing || passenger.State != PassengerState.Idle)
                return;

            if (!reachable[board.ToIndex(passenger.Cell)])
            {
                passenger.ShowAngry();
                return;
            }

            bool hasFreeSlot = waitingArea.HasFreeSlot;
            Bus bus = busStation.ActiveBus;
            bool toBus = hasFreeSlot && bus != null && bus.IsAtDoor && bus.Color == passenger.Color && !bus.IsReservedFull;
            bool toOverflow = !hasFreeSlot && SlotWillOpen();

            // Slot yok ve ileride de açılmayacaksa dokunma tamamen yok sayılır (Asen kararı, 19 Kasım; ZD2).
            if (!hasFreeSlot && !toOverflow)
                return;

            passenger.ShowTap();
            Vector2Int startCell = passenger.Cell;
            board.SetOccupant(startCell, null);
            TryEmergeFromTunnel(startCell);
            RefreshReachable();

            board.TryFindPath(startCell, pathBuffer);
            Vector2Int lastCell = pathBuffer.Count > 0 ? pathBuffer[pathBuffer.Count - 1] : startCell;

            worldPathBuffer.Clear();
            for (int i = 0; i < pathBuffer.Count; i++)
                worldPathBuffer.Add(boardView.CellToWorld(pathBuffer[i]));
            worldPathBuffer.Add(boardView.CellToWorld(new Vector2Int(lastCell.x, -1)));

            runningCount++;

            if (toBus)
            {
                Transform seatPoint = bus.ReserveSeat(out int seatIndex);
                worldPathBuffer.Add(bus.DoorEntry.position);
                passenger.RunTo(worldPathBuffer, runSpeed, () =>
                {
                    passenger.EnterBus(bus.DoorEntry, seatPoint, () => OnSeatArrived(bus, seatIndex, passenger));
                });
            }
            else if (hasFreeSlot)
            {
                waitingArea.TryReserveSlot(passenger, out Vector3 slotPos);
                worldPathBuffer.Add(slotPos);
                passenger.RunTo(worldPathBuffer, runSpeed, () =>
                {
                    passenger.SetInSlot(waitingArea.WaitingRotation, waitingArea.PassengerSlotScale);
                    Bus atDoor = busStation.ActiveBus;
                    if (atDoor != null && atDoor.IsAtDoor && atDoor.Color == passenger.Color)
                        BoardWaitingPassengers(atDoor);
                    OnRunFinished();
                });
            }
            else
            {
                // Slotlar dolu ama bir slot açılacak: taşma kuyruğuna girer, arkada bekler (Asen kararı, 19 Kasım; ZD2).
                int queueIndex = overflowQueue.Count;
                overflowQueue.Add(passenger);
                worldPathBuffer.Add(waitingArea.OverflowPosition(queueIndex));
                passenger.RunTo(worldPathBuffer, runSpeed, () =>
                {
                    if (passenger.PendingSlotIndex >= 0)
                    {
                        int slotIndex = passenger.PendingSlotIndex;
                        passenger.PendingSlotIndex = -1;
                        RunToSlot(passenger, slotIndex, false);
                    }
                    else
                    {
                        passenger.SetIdle(waitingArea.WaitingRotation);
                        OnRunFinished();
                    }
                });
            }
        }

        /// <summary>Devam eden koşular ve otobüs gelişleri sonunda bir slot açılacak mı (Asen kararı, 19 Kasım; ZD2).</summary>
        private bool SlotWillOpen()
        {
            Bus active = busStation.ActiveBus;
            if (active == null)
                return false;

            if (!active.IsReservedFull && waitingArea.HasQueuedColor(active.Color))
                return true;

            if (active.IsReservedFull)
            {
                Bus next = busStation.NextBus;
                if (next != null && waitingArea.HasQueuedColor(next.Color))
                    return true;
            }

            return false;
        }

        // Bir slot boşaldı: taşma kuyruğunun başındaki yolcuya hemen bu slot ayrılır. Idle ise (zaten bekliyordu)
        // hemen slota koşar; hâlâ koşuyorsa (overflow noktasına gidiyor) varışta kontrol edip oraya gider.
        // Kalan kuyruk bir adım öne kayar (zaten bekleyenler için kısa tween).
        private void OnSlotOpened(int slotIndex)
        {
            if (overflowQueue.Count == 0)
                return;

            Passenger passenger = overflowQueue[0];
            overflowQueue.RemoveAt(0);
            waitingArea.ReserveSlotDirect(slotIndex, passenger);

            if (passenger.State == PassengerState.Idle)
                RunToSlot(passenger, slotIndex, true);
            else
                passenger.PendingSlotIndex = slotIndex;

            for (int i = 0; i < overflowQueue.Count; i++)
            {
                Passenger waiting = overflowQueue[i];
                if (waiting.State == PassengerState.Idle)
                    waiting.ShiftOverflow(waitingArea.OverflowPosition(i));
            }
        }

        // isNewRun: true ise yeni bir koşu başlıyor (runningCount++); false ise taşma koşusunun hedefi slota
        // çevrildi, sayaç zaten o koşu için sayılıydı.
        private void RunToSlot(Passenger passenger, int slotIndex, bool isNewRun)
        {
            if (isNewRun)
                runningCount++;

            worldPathBuffer.Clear();
            worldPathBuffer.Add(waitingArea.SlotPosition(slotIndex));
            passenger.RunTo(worldPathBuffer, runSpeed, () =>
            {
                passenger.SetInSlot(waitingArea.WaitingRotation, waitingArea.PassengerSlotScale);
                Bus atDoor = busStation.ActiveBus;
                if (atDoor != null && atDoor.IsAtDoor && atDoor.Color == passenger.Color)
                    BoardWaitingPassengers(atDoor);
                OnRunFinished();
            });
        }

        // Otobüs gelince (ilk kurulum dahil) slottaki aynı renk yolcular kendiliğinden biner.
        private void BoardWaitingPassengers(Bus bus)
        {
            if (gameManager.State != GameState.Playing)
                return;

            runningCount += waitingArea.SendMatchingPassengers(bus, OnSeatArrived);

            // Eşleşme yoksa kimse koşmadı, runningCount değişmedi; fail yine de burada kontrol edilmeli.
            CheckFail();
        }

        private void OnSeatArrived(Bus bus, int seatIndex, Passenger passenger)
        {
            if (gameManager.State == GameState.Playing)
                busStation.NotifySeatArrived(bus, seatIndex, passenger);
            OnRunFinished();
        }

        // Otobüs ayrılış tween'i bitti: içindeki yolcular BoardView havuzuna, otobüsün kendisi BusStation havuzuna döner.
        private void OnBusDeparted(Bus bus)
        {
            Passenger[] seated = bus.SeatedPassengers;
            for (int i = 0; i < seated.Length; i++)
            {
                if (seated[i] != null)
                    boardView.ReleasePassenger(seated[i]);
            }

            busStation.ReleaseBus(bus);
        }

        private void OnAllBusesDone()
        {
            if (gameManager.State != GameState.Playing)
                return;

            LevelCompleted?.Invoke();
            gameManager.Complete();
        }

        private void OnRunFinished()
        {
            runningCount--;
            CheckFail();
        }

        // Devam eden koşular ve otobüsün kapıya gelişi sonuçlandıktan sonra kalan slotlara bakılır.
        private void CheckFail()
        {
            if (gameManager.State != GameState.Playing || runningCount > 0 || waitingArea.HasFreeSlot)
                return;

            Bus bus = busStation.ActiveBus;
            if (bus == null || !bus.IsAtDoor)
                return;

            LevelFailed?.Invoke(FailReason.NoSpace);
            gameManager.Fail(FailReason.NoSpace);
        }

        private void RefreshReachable()
        {
            board.ComputeReachable(reachable);
            for (int i = 0; i < board.CellCount; i++)
            {
                Passenger p = board.GetOccupant(i);
                if (p == null)
                    continue;

                p.SetReachable(reachable[i]);
                if (reachable[i] && p.IsHidden)
                    p.Reveal(boardView.GetColorMaterial(p.Color));
            }
        }
    }
}
