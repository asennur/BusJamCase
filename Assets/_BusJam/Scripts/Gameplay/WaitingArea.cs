using System;
using System.Collections.Generic;
using BusJam.Core;
using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Bekleme slotları: her zaman 7 karo (Asen kuralı), uçtaki ikisi level numarasına göre kilitli/açık.
    /// Rezervasyon, slot pozisyonu, otobüs gelince aynı renk yolcuları otobüse koşturur.
    /// Taşma kuyruğu (slotlar doluyken dokunulan yolcular) GameplayController'da tutulur; burası yalnız
    /// slot açıldığını bildirir (SlotOpened) ve dışarıdan verilen yolcuyu doğrudan bir slota yazar.
    /// </summary>
    public class WaitingArea : MonoBehaviour
    {
        [SerializeField] private SlotTile slotTilePrefab;
        [Tooltip("Eski placeholder kilit ikonu; kit modelinde kilit SlotTile içinde, boş bırakılabilir")]
        [SerializeField] private GameObject lockIconPrefab;
        [SerializeField] private float slotSpacing = 1.003f;
        [SerializeField] private float overflowBaseDistance = 0.9f;
        [SerializeField] private float overflowSpacing = 0.5f;
        [SerializeField] private float runSpeed = 6.72f;

        private readonly Passenger[] slots = new Passenger[GameRules.VisibleSlotCount];
        private readonly bool[] unlocked = new bool[GameRules.VisibleSlotCount];
        private readonly List<SlotTile> tiles = new List<SlotTile>();
        private readonly List<GameObject> lockIcons = new List<GameObject>();
        private readonly List<Vector3> singlePointPath = new List<Vector3>(1) { Vector3.zero };

        /// <summary>Bir slot otobüse biniş yüzünden boşalınca slotIndex ile atılır (GameplayController taşma kuyruğunu bu slota atar).</summary>
        public event Action<int> SlotOpened;
        public Quaternion WaitingRotation => transform.rotation;

        public bool HasFreeSlot
        {
            get
            {
                for (int i = 0; i < slots.Length; i++)
                    if (unlocked[i] && slots[i] == null)
                        return true;
                return false;
            }
        }

        // Karolar her levelde aynı 7 tane; yalnız kilit durumu ve görünümü güncellenir, Destroy/Instantiate yok.
        public void Setup(int levelNumber)
        {
            Array.Clear(slots, 0, slots.Length);

            for (int i = 0; i < GameRules.VisibleSlotCount; i++)
            {
                unlocked[i] = GameRules.IsSlotUnlocked(i, levelNumber);

                if (i >= tiles.Count)
                {
                    SlotTile tile = Instantiate(slotTilePrefab, transform);
                    tiles.Add(tile);
                    lockIcons.Add(lockIconPrefab != null ? Instantiate(lockIconPrefab, tile.transform, false) : null);
                }

                int unlockLevel = i == 0 ? GameRules.FirstSlotUnlockLevel : GameRules.LastSlotUnlockLevel;
                tiles[i].transform.position = SlotPosition(i);
                tiles[i].SetLocked(!unlocked[i], unlockLevel);
                if (lockIcons[i] != null)
                    lockIcons[i].SetActive(!unlocked[i]);
            }
        }

        public Vector3 SlotPosition(int index) =>
            transform.TransformPoint(new Vector3((index - (GameRules.VisibleSlotCount - 1) * 0.5f) * slotSpacing, 0f, 0f));

        /// <summary>Taşma kuyruğundaki sıraya (0 = en önde) göre, slot şeridinin grid tarafında dizilen nokta (Asen kararı, 19 Kasım).</summary>
        public Vector3 OverflowPosition(int queueIndex) =>
            transform.TransformPoint(new Vector3(0f, 0f, -(overflowBaseDistance + queueIndex * overflowSpacing)));

        /// <summary>Slottaki ya da slota koşmakta olan (rezerve edilmiş) bir yolcu bu renkte mi.</summary>
        public bool HasQueuedColor(PassengerColor color)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null && slots[i].Color == color)
                    return true;
            return false;
        }

        /// <summary>Boş ve açık slotu hemen rezerve eder (passenger referansı anında yazılır).</summary>
        public bool TryReserveSlot(Passenger passenger, out Vector3 worldPos)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (unlocked[i] && slots[i] == null)
                {
                    slots[i] = passenger;
                    worldPos = SlotPosition(i);
                    return true;
                }
            }

            worldPos = default;
            return false;
        }

        /// <summary>Taşma kuyruğundan gelen yolcuyu doğrudan bu slota yazar (GameplayController zaten hangi slotun boşaldığını SlotOpened ile öğrendi).</summary>
        public void ReserveSlotDirect(int slotIndex, Passenger passenger) => slots[slotIndex] = passenger;

        /// <summary>Yerleşme sırasında eşleşen slot yolcularını başlatır; biniş fiziksel varışı bekler.</summary>
        public int SendMatchingPassengers(Bus bus, Action<Bus, int, Passenger> onSeatArrived)
        {
            if (!bus.CanApproach) return 0;
            int startedCount = 0;

            for (int i = 0; i < slots.Length && !bus.IsReservedFull; i++)
            {
                Passenger passenger = slots[i];
                if (passenger == null || passenger.State != PassengerState.InSlot || passenger.Color != bus.Color)
                    continue;

                slots[i] = null;
                SlotOpened?.Invoke(i);
                bus.ReserveSeat(out int seatIndex);
                startedCount++;

                singlePointPath[0] = bus.ArrivalEntryPosition;
                float runTime = passenger.RunTo(singlePointPath, runSpeed, () =>
                {
                    bus.BoardWhenReady(seatIndex, passenger, () => onSeatArrived(bus, seatIndex, passenger));
                });
                bus.OpenDoorBefore(runTime);
            }

            return startedCount;
        }
    }
}
