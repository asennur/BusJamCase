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
        private List<PassengerColor> order;
        private int nextSpawnIndex;
        private ObjectPool<Bus> busPool;

        public Bus ActiveBus => queue.Count > 0 ? queue[0] : null;

        public event Action<Bus> BusArrived;
        public event Action<Bus> BusDeparted;
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
                busPool.Release(queue[i]);
            queue.Clear();

            order = busOrder;
            nextSpawnIndex = 0;
            for (int i = 0; i < visibleQueueCount && nextSpawnIndex < order.Count; i++)
                SpawnAtQueueEnd();

            if (queue.Count > 0)
            {
                queue[0].SetAtDoor(true);
                BusArrived?.Invoke(queue[0]);
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
                DepartActiveBus();
        }

        /// <summary>Yolcuları boşaltılmış otobüsü havuza geri verir (BusDeparted sonrası GameplayController çağırır).</summary>
        public void ReleaseBus(Bus bus) => busPool.Release(bus);

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

            leaving.transform.DOMove(leaving.transform.position + doorPoint.right * exitOffsetX, moveDuration)
                .SetEase(Ease.InSine)
                .OnComplete(() => BusDeparted?.Invoke(leaving));

            for (int i = 0; i < queue.Count; i++)
            {
                Vector3 target = QueuePosition(i);
                Tween move = queue[i].transform.DOMove(target, moveDuration).SetEase(Ease.OutSine);

                if (i == 0)
                {
                    Bus arriving = queue[0];
                    move.OnComplete(() =>
                    {
                        arriving.SetAtDoor(true);
                        BusArrived?.Invoke(arriving);
                    });
                }
            }

            if (nextSpawnIndex < order.Count)
                SpawnAtQueueEnd();

            if (queue.Count == 0)
                AllBusesDone?.Invoke();
        }
    }
}
