using System;
using System.Collections.Generic;
using BusJam.Core;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Bekleme slotları: her zaman 7 karo (Asen kuralı), uçtaki ikisi level numarasına göre kilitli/açık.
    /// Rezervasyon, slot pozisyonu, otobüs gelince aynı renk yolcuları otobüse koşturur.
    /// </summary>
    public class WaitingArea : MonoBehaviour
    {
        [SerializeField] private Transform slotTilePrefab;
        [SerializeField] private Material lockedMaterial;
        [SerializeField] private GameObject lockIconPrefab;
        [SerializeField] private float slotSpacing = 0.85f;
        [SerializeField] private float overflowBaseDistance = 0.9f;
        [SerializeField] private float overflowSpacing = 0.5f;
        [SerializeField] private float runSpeed = 4f;

        private readonly Passenger[] slots = new Passenger[GameRules.VisibleSlotCount];
        private readonly bool[] unlocked = new bool[GameRules.VisibleSlotCount];
        private readonly List<Transform> tiles = new List<Transform>();
        private readonly List<Renderer> tileRenderers = new List<Renderer>();
        private readonly List<GameObject> lockIcons = new List<GameObject>();
        private readonly List<Vector3> singlePointPath = new List<Vector3>(1) { Vector3.zero };
        private Material unlockedMaterial;
        private int overflowCount;

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
            overflowCount = 0;
            Array.Clear(slots, 0, slots.Length);

            for (int i = 0; i < GameRules.VisibleSlotCount; i++)
            {
                unlocked[i] = GameRules.IsSlotUnlocked(i, levelNumber);

                if (i >= tiles.Count)
                {
                    Transform tile = Instantiate(slotTilePrefab, SlotPosition(i), Quaternion.identity, transform);
                    Renderer renderer = tile.GetComponentInChildren<Renderer>();
                    if (unlockedMaterial == null)
                        unlockedMaterial = renderer.sharedMaterial;
                    tiles.Add(tile);
                    tileRenderers.Add(renderer);
                    lockIcons.Add(Instantiate(lockIconPrefab, tile, false));
                }

                tiles[i].position = SlotPosition(i);
                tileRenderers[i].sharedMaterial = unlocked[i] ? unlockedMaterial : lockedMaterial;
                lockIcons[i].SetActive(!unlocked[i]);
            }
        }

        public Vector3 SlotPosition(int index) =>
            transform.TransformPoint(new Vector3((index - (GameRules.VisibleSlotCount - 1) * 0.5f) * slotSpacing, 0f, 0f));

        /// <summary>Slotlar doluyken dokunulan yolcunun koşacağı, slot sırasının grid tarafına dizilen nokta (Asen kararı, 19 Kasım).</summary>
        public Vector3 OverflowPosition()
        {
            Vector3 point = transform.TransformPoint(new Vector3(0f, 0f, -(overflowBaseDistance + overflowCount * overflowSpacing)));
            overflowCount++;
            return point;
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

        /// <summary>Kapıdaki (IsAtDoor) otobüsün rengiyle eşleşen slottaki yolcuları, boş koltuk kalmayana kadar otobüse koşturur. Döndürdüğü sayı, koşmaya başlayan yolcu sayısıdır (çağıran runningCount'u buna göre artırmalı).</summary>
        public int SendMatchingPassengers(Bus bus, Action<Bus, int, Passenger> onSeatArrived)
        {
            int startedCount = 0;

            for (int i = 0; i < slots.Length && !bus.IsReservedFull; i++)
            {
                Passenger passenger = slots[i];
                if (passenger == null || passenger.State != PassengerState.InSlot || passenger.Color != bus.Color)
                    continue;

                slots[i] = null;
                Transform seatPoint = bus.ReserveSeat(out int seatIndex);
                startedCount++;

                singlePointPath[0] = seatPoint.position;
                passenger.RunTo(singlePointPath, runSpeed, () =>
                {
                    passenger.SetSeated(seatPoint);
                    onSeatArrived(bus, seatIndex, passenger);
                });
            }

            return startedCount;
        }
    }
}
