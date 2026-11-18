using System;
using System.Collections.Generic;
using BusJam.Core;
using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Kuralların tek yeri: dokunma kararı, hedef rezervasyonu, erişilebilirlik, kazan/kaybet.
    /// Hedef sırası: otobüs (kapıda + aynı renk + boş koltuk) → bekleme slotu → yoksa kızgın yüz.
    /// </summary>
    public class GameplayController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private BoardView boardView;
        [SerializeField] private BusStation busStation;
        [SerializeField] private WaitingArea waitingArea;
        [SerializeField] private float runSpeed = 4f;

        private Board board;
        private bool[] reachable;
        private int runningCount;
        private readonly List<Vector2Int> pathBuffer = new List<Vector2Int>();
        private readonly List<Vector3> worldPathBuffer = new List<Vector3>();
        private readonly List<TunnelGroup> tunnelGroups = new List<TunnelGroup>();

        // Çalışma zamanı tünel durumu: level verisindeki kuyruk burada tüketilir, asset değişmez.
        private class TunnelGroup
        {
            public Vector2Int tunnelCell;
            public Vector2Int mouthCell;
            public Queue<PassengerColor> queue;
            public TunnelView view;
        }

        public event Action LevelCompleted;
        public event Action<string> LevelFailed;

        private void OnEnable()
        {
            gameManager.LevelLoaded += OnLevelLoaded;
            inputHandler.PassengerTapped += OnPassengerTapped;
            busStation.BusArrived += OnBusArrived;
            busStation.BusDeparted += OnBusDeparted;
            busStation.AllBusesDone += OnAllBusesDone;
        }

        private void OnDisable()
        {
            gameManager.LevelLoaded -= OnLevelLoaded;
            inputHandler.PassengerTapped -= OnPassengerTapped;
            busStation.BusArrived -= OnBusArrived;
            busStation.BusDeparted -= OnBusDeparted;
            busStation.AllBusesDone -= OnAllBusesDone;
        }

        private void OnLevelLoaded(int levelNumber, LevelData level)
        {
            board = gameManager.Board;
            if (reachable == null || reachable.Length < board.CellCount)
                reachable = new bool[board.CellCount];

            runningCount = 0;
            SetupTunnels(level);
            waitingArea.Setup(level.waitingSlotCount);
            busStation.Setup(level.busOrder);
            RefreshReachable();
        }

        private void SetupTunnels(LevelData level)
        {
            tunnelGroups.Clear();
            for (int i = 0; i < level.tunnels.Count; i++)
            {
                TunnelData data = level.tunnels[i];
                tunnelGroups.Add(new TunnelGroup
                {
                    tunnelCell = data.cell,
                    mouthCell = data.MouthCell,
                    queue = new Queue<PassengerColor>(data.queue),
                    view = boardView.GetTunnelView(data.cell)
                });
            }
        }

        // Dokunulan yolcu hücresini bıraktığı an çağrılır: ağız o hücreyse kuyruktaki sıradaki yolcu çıkar.
        private void TryEmergeFromTunnel(Vector2Int vacatedCell)
        {
            for (int i = 0; i < tunnelGroups.Count; i++)
            {
                TunnelGroup group = tunnelGroups[i];
                if (group.mouthCell != vacatedCell || group.queue.Count == 0)
                    continue;

                PassengerColor color = group.queue.Dequeue();
                Passenger passenger = boardView.SpawnFromTunnel(group.tunnelCell, vacatedCell, color);
                board.SetOccupant(vacatedCell, passenger);
                group.view?.SetRemaining(group.queue.Count);
                return;
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

            Bus bus = busStation.ActiveBus;
            bool toBus = bus != null && bus.IsAtDoor && bus.Color == passenger.Color && !bus.IsReservedFull;

            if (!toBus && !waitingArea.HasFreeSlot)
            {
                passenger.ShowAngry();
                return;
            }

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
                worldPathBuffer.Add(seatPoint.position);
                passenger.RunTo(worldPathBuffer, runSpeed, () =>
                {
                    passenger.SetSeated(seatPoint);
                    busStation.NotifySeatArrived(bus, seatIndex, passenger);
                    OnRunFinished();
                });
            }
            else
            {
                waitingArea.TryReserveSlot(passenger, out Vector3 slotPos);
                worldPathBuffer.Add(slotPos);
                passenger.RunTo(worldPathBuffer, runSpeed, () =>
                {
                    passenger.SetInSlot();
                    OnRunFinished();
                });
            }
        }

        // Otobüs gelince (ilk kurulum dahil) slottaki aynı renk yolcular kendiliğinden biner.
        private void OnBusArrived(Bus bus)
        {
            int started = waitingArea.SendMatchingPassengers(bus, (b, seatIndex, passenger) =>
            {
                busStation.NotifySeatArrived(b, seatIndex, passenger);
                OnRunFinished();
            });
            runningCount += started;

            // Eşleşme yoksa kimse koşmadı, runningCount değişmedi; fail yine de burada kontrol edilmeli.
            CheckFail();
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
            LevelCompleted?.Invoke();
            gameManager.Complete();
        }

        private void OnRunFinished()
        {
            runningCount--;
            CheckFail();
        }

        // Kaybet (yer): koşan yolcu yok, slotlar dolu, slottakilerin hiçbiri kapıdaki otobüsün rengine
        // uymuyor ve gridde o renkten erişilebilir yolcu da yok (varsa oyuncu hâlâ kurtarabilir).
        private void CheckFail()
        {
            if (runningCount > 0 || waitingArea.HasFreeSlot)
                return;

            Bus bus = busStation.ActiveBus;
            if (bus == null)
                return;

            if (waitingArea.HasColor(bus.Color) || HasReachableColor(bus.Color))
                return;

            string reason = $"Slotlar dolu, {bus.Color} otobüsüne uyan yolcu yok";
            Debug.Log($"[GameplayController] Fail: {reason}");
            LevelFailed?.Invoke(reason);
            gameManager.Fail();
        }

        private bool HasReachableColor(PassengerColor color)
        {
            for (int i = 0; i < board.CellCount; i++)
            {
                if (!reachable[i])
                    continue;

                Passenger p = board.GetOccupant(i);
                if (p != null && !p.IsHidden && p.Color == color)
                    return true;
            }

            return false;
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
