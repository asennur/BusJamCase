using System;
using System.Collections.Generic;
using BusJam.Level;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Kalkışı ve kuyruk hareketini üst üste bindirir; biniş fiziksel varışa bağlı kalır.
    /// </summary>
    public class BusStation : MonoBehaviour
    {
        [SerializeField] private Bus busPrefab;
        [Tooltip("In PassengerColor order.")]
        [SerializeField] private Material[] colorMaterials;
        [SerializeField] private Transform doorPoint;
        [SerializeField] private float queueSpacing = 2.2f;
        [SerializeField] private float exitOffsetX = 6f;
        [SerializeField] private float moveDuration = 0.3f;
        [Tooltip("Visible buses, including the bus at the door.")]
        [SerializeField] private int visibleQueueCount = 3;
        [Tooltip("Shared world-space exhaust; puffs outlive the pooled bus that emitted them.")]
        [SerializeField] private ParticleSystem exhaust;

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
        public event Action<Bus> BusApproaching;
        public event Action<Bus> BusFilled;
        public event Action<Bus> BusDeparted;
        /// <summary>Dolu bir otobüs (kapısı kapandıktan sonra) uzaklaşmaya başlar; kalkış sesi için kullanılır.</summary>
        public event Action<Bus> BusDeparting;
        public event Action AllBusesDone;

        // Kuyruk + kalkıştaki iki otobüs baştan üretilir; oyun ortasında Instantiate takılması olmaz.
        private void EnsurePool()
        {
            if (busPool != null)
                return;

            busPool = new ObjectPool<Bus>(
                CreateBus,
                b => b.gameObject.SetActive(true),
                b => { b.gameObject.SetActive(false); b.transform.SetParent(transform, false); });
            for (int i = 0; i < visibleQueueCount + 2; i++)
                busPool.Release(CreateBus());
        }

        private Bus CreateBus()
        {
            Bus bus = Instantiate(busPrefab, transform);
            bus.Exhaust = exhaust;
            return bus;
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

            // Kapı kapanışı hazırlık süresini paylaşır; son tamamlanma olayı hemen atılır.
            float delay = finalBus ? 0f : Mathf.Max(0.08f, leaving.CloseDoor());
            leaving.AnticipateDeparture();
            departing.Add(leaving);
            Vector3 exit = leaving.transform.position + doorPoint.right * exitOffsetX;
            Tween departure = null;
            departure = leaving.transform.DOMove(exit, moveDuration)
                .SetDelay(delay)
                .SetEase(departureEase)
                .SetRecyclable(true)
                .OnStart(() => { leaving.BeginMotion(exit, false); if (!finalBus) BusDeparting?.Invoke(leaving); })
                .OnUpdate(() => leaving.SampleMotion(departure.ElapsedPercentage()))
                .OnComplete(() => { leaving.EndMotion(); BusDeparted?.Invoke(leaving); });

            // %60 örtüşme 4.12 birimlik gövdeler arasındaki boşluğu korur.
            float queueDelay = delay + moveDuration * 0.4f;
            for (int i = 0; i < queue.Count; i++)
                MoveToQueuePosition(i, queueDelay + i * 0.04f);

            if (nextSpawnIndex < order.Count)
            {
                SpawnAtQueueEnd();
                int tail = queue.Count - 1;
                queue[tail].transform.position = QueuePosition(tail + 1);
                MoveToQueuePosition(tail, queueDelay + tail * 0.04f);
            }

            if (finalBus)
                AllBusesDone?.Invoke();
        }

        private void MoveToQueuePosition(int index, float delay = 0f)
        {
            Bus bus = queue[index];
            bus.transform.DOKill();
            Vector3 destination = QueuePosition(index);
            bool approachSent = false;
            Tween motion = null;
            motion = bus.transform.DOMove(destination, moveDuration)
                .SetDelay(delay).SetEase(arrivalEase).SetRecyclable(true)
                .OnStart(() => bus.BeginMotion(destination, true))
                .OnUpdate(() =>
                {
                    float t = motion.ElapsedPercentage();
                    float distanceProgress = DOVirtual.EasedValue(0f, 1f, t, arrivalEase);
                    bus.SampleMotion(t);
                    if (index == 0 && !approachSent && distanceProgress >= 0.85f)
                    {
                        approachSent = true;
                        bus.AllowApproach();
                        BusApproaching?.Invoke(bus);
                    }
                })
                .OnComplete(() =>
                {
                    bus.EndMotion();
                    if (index == 0)
                    {
                        bus.SetAtDoor(true);
                        BusArrived?.Invoke(bus);
                    }
                    else bus.PlayStop();
                });
        }
    }
}
