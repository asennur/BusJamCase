using System;
using System.Collections.Generic;
using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Bekleme slotları: rezervasyon, slot pozisyonu, otobüs gelince aynı renk yolcuları otobüse koşturur.
    /// </summary>
    public class WaitingArea : MonoBehaviour
    {
        [SerializeField] private Transform slotTilePrefab;
        [SerializeField] private float slotSpacing = 0.85f;
        [SerializeField] private float runSpeed = 4f;

        private Passenger[] slots;
        private readonly List<Transform> tiles = new List<Transform>();
        private readonly List<Vector3> singlePointPath = new List<Vector3>(1) { Vector3.zero };

        public bool HasFreeSlot
        {
            get
            {
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i] == null)
                        return true;
                return false;
            }
        }

        // Karolar levelden levele yeniden kullanılır (yalnız sayı değişince eklenir/kapatılır), Destroy/Instantiate yok.
        public void Setup(int slotCount)
        {
            slots = new Passenger[slotCount];

            for (int i = 0; i < slotCount; i++)
            {
                if (i < tiles.Count)
                {
                    tiles[i].position = SlotPosition(i);
                    tiles[i].gameObject.SetActive(true);
                }
                else
                {
                    tiles.Add(Instantiate(slotTilePrefab, SlotPosition(i), Quaternion.identity, transform));
                }
            }

            for (int i = slotCount; i < tiles.Count; i++)
                tiles[i].gameObject.SetActive(false);
        }

        public Vector3 SlotPosition(int index) =>
            transform.TransformPoint(new Vector3((index - (slots.Length - 1) * 0.5f) * slotSpacing, 0f, 0f));

        /// <summary>Boş slotu hemen rezerve eder (passenger referansı anında yazılır).</summary>
        public bool TryReserveSlot(Passenger passenger, out Vector3 worldPos)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = passenger;
                    worldPos = SlotPosition(i);
                    return true;
                }
            }

            worldPos = default;
            return false;
        }

        public bool HasColor(PassengerColor color)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null && slots[i].Color == color)
                    return true;
            return false;
        }

        /// <summary>Kapıdaki (IsAtDoor) otobüsün rengiyle eşleşen slottaki yolcuları, boş koltuk kalmayana kadar otobüse koşturur. Döndürdüğü sayı, koşmaya başlayan yolcu sayısıdır (çağıran runningCount'u buna göre artırmalı).</summary>
        public int SendMatchingPassengers(Bus bus, Action<Bus, int, Passenger> onSeatArrived)
        {
            int startedCount = 0;

            for (int i = 0; i < slots.Length && !bus.IsReservedFull; i++)
            {
                Passenger passenger = slots[i];
                if (passenger == null || passenger.Color != bus.Color)
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
