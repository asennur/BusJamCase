using System;
using System.Collections.Generic;
using BusJam.Level;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Otobüs sırası ve kapıdaki aktif otobüs. Dolunca kapıdaki ayrılır, kuyruk ilerler, sıradaki gelir.
    /// BusArrived yalnız otobüs fiilen kapıya varınca (kuyruk tween'i bitince) atılır.
    /// </summary>
    public class BusStation : MonoBehaviour
    {
        [SerializeField] private Bus busPrefab;
        [Tooltip("PassengerColor sırasıyla")]
        [SerializeField] private Material[] colorMaterials;
        [SerializeField] private Transform doorPoint;
        [SerializeField] private float queueSpacing = 2.2f;
        [SerializeField] private float exitOffsetX = 6f;
        [SerializeField] private float moveDuration = 0.6f;
        [Tooltip("Kapıda + kuyrukta aynı anda sahnede duran otobüs sayısı")]
        [SerializeField] private int visibleQueueCount = 3;

        private readonly List<Bus> queue = new List<Bus>();
        [SerializeField] private Ease arrivalEase = Ease.OutCubic;
        [SerializeField] private Ease departureEase = Ease.InQuad;
        private readonly List<Bus> departing = new List<Bus>();
        private List<PassengerColor> order;
        private int nextSpawnIndex;
        private ObjectPool<Bus> busPool;

        public Bus ActiveBus => queue.Count > 0 ? queue[0] : null;
        public bool IsTransitioning => departing.Count > 0 || (ActiveBus != null && !ActiveBus.IsAtDoor);

        /// <summary>Kapıdaki/kapıya gelmekte olan aktif otobüsten sonra kuyrukta duran otobüs (yoksa null).</summary>
        public Bus NextBus => queue.Count > 1 ? queue[1] : null;

        public event Action<Bus> BusArrived;
        public event Action<Bus> BusFilled;
        public event Action<Bus> BusDeparted;
        /// <summary>A full bus starts driving away (after its door closed); used for the departure sound.</summary>
        public event Action<Bus> BusDeparting;
        public event Action AllBusesDone;

        private void EnsurePool()
        {
            if (busPool != null)
                return;

            busPool = new ObjectPool<Bus>(
                () => Instantiate(busPrefab, transform),
                b => b.gameObject.SetActive(true),
                b => { b.gameObject.SetActive(false); b.transform.SetParent(transform, false); });
        }

        public void Setup(List<PassengerColor> busOrder)
        {
            EnsurePool();
            for (int i = 0; i < queue.Count; i++)
                ReturnToPool(queue[i]);
            foreach (Bus bus in departing)
                ReturnToPool(bus);
            departing.Clear();
            queue.Clear();

            order = busOrder;
            nextSpawnIndex = 0;
            for (int i = 0; i < visibleQueueCount && nextSpawnIndex < order.Count; i++)
                SpawnAtQueueEnd();

            if (queue.Count > 0)
            {
                for (int i = 0; i < queue.Count; i++)
                {
                    queue[i].transform.position = QueuePosition(i + 1);
                    MoveToQueuePosition(i);
                }
            }
            else
            {
                AllBusesDone?.Invoke();
            }
        }

        /// <summary>Yolcu koltuğa vardı; otobüs tam dolduysa kapıdakini gönderir, sıradakini getirir.</summary>
        public void NotifySeatArrived(Bus bus, int seatIndex, Passenger passenger)
        {
            if (bus.SeatArrived(seatIndex, passenger))
            {
                BusFilled?.Invoke(bus);
                DepartActiveBus();
            }
        }

        /// <summary>Yolcuları boşaltılmış otobüsü havuza geri verir (BusDeparted sonrası GameplayController çağırır).</summary>
        public void ReleaseBus(Bus bus)
        {
            departing.Remove(bus);
            busPool.Release(bus);
        }

        private void ReturnToPool(Bus bus)
        {
            bus.transform.DOKill();
            busPool.Release(bus);
        }

        private void SpawnAtQueueEnd()
        {
            Bus bus = busPool.Get();
            PassengerColor color = order[nextSpawnIndex];
            bus.Setup(color, colorMaterials[(int)color]);
            bus.transform.position = QueuePosition(queue.Count);
            queue.Add(bus);
            nextSpawnIndex++;
        }

        private Vector3 QueuePosition(int queueIndex) =>
            doorPoint.position - doorPoint.right * (queueSpacing * queueIndex);

        private void DepartActiveBus()
        {
            Bus leaving = queue[0];
            leaving.SetAtDoor(false);
            queue.RemoveAt(0);

            bool finalBus = queue.Count == 0 && nextSpawnIndex == order.Count;
            if (finalBus)
                foreach (Passenger passenger in leaving.SeatedPassengers)
                    passenger.Wave();

            // Normal buses wait for the door to close. The last bus leaves at once and silently,
            // because the Complete screen opens right away (Asen, 19 Nov).
            float delay = finalBus ? 0f : leaving.CloseDoor();
            departing.Add(leaving);
            leaving.transform.DOMove(leaving.transform.position + doorPoint.right * exitOffsetX, moveDuration)
                .SetDelay(delay)
                .SetEase(departureEase)
                .OnStart(() => { leaving.PlayDrive(); if (!finalBus) BusDeparting?.Invoke(leaving); })
                .OnComplete(() => BusDeparted?.Invoke(leaving));

            // Çıkış tamamlanınca kapıdan 6 birim uzaklaşmış olur (gövde 4.12 + boşluk).
            float queueDelay = delay + moveDuration;
            for (int i = 0; i < queue.Count; i++)
                MoveToQueuePosition(i, queueDelay);

            if (nextSpawnIndex < order.Count)
            {
                SpawnAtQueueEnd();
                int tail = queue.Count - 1;
                queue[tail].transform.position = QueuePosition(tail + 1);
                MoveToQueuePosition(tail, queueDelay);
            }

            if (finalBus)
                AllBusesDone?.Invoke();
        }

        private void MoveToQueuePosition(int index, float delay = 0f)
        {
            Bus bus = queue[index];
            bus.transform.DOKill();
            Tween motion = bus.transform.DOMove(QueuePosition(index), moveDuration)
                .SetDelay(delay).SetEase(arrivalEase).OnStart(bus.PlayDrive);
            if (index == 0)
                motion.OnComplete(() =>
                {
                    bus.PlayStop(true, () =>
                    {
                        bus.SetAtDoor(true);
                        BusArrived?.Invoke(bus);
                    });
                });
            else
                motion.OnComplete(() => bus.PlayStop(false));
        }
    }
}
