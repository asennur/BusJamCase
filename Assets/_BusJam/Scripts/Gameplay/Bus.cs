using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Tek otobüsün görünümü ve koltuk durumu. Rezervasyon (dokunma anında) ve varış (yolcu koltuğa
    /// ışınlanınca) ayrıdır: gösterge yalnız yolcu gerçekten varınca dolu görünür. Varan yolcular koltuğa
    /// parent'lanır (Passenger.SetSeated), bu yüzden otobüs onları kendileriyle birlikte taşır.
    /// </summary>
    public class Bus : MonoBehaviour
    {
        [SerializeField] private Renderer body;
        [SerializeField] private Transform[] seatPoints;
        [SerializeField] private GameObject[] seatIndicators;

        private int reservedCount;
        private int arrivedCount;
        private Passenger[] seatedPassengers;

        public PassengerColor Color { get; private set; }
        public bool IsReservedFull => reservedCount >= seatPoints.Length;

        /// <summary>Kuyruktan kapıya gelme tween'i bitince true olur; yolcular yalnız o zaman bu otobüse koşabilir.</summary>
        public bool IsAtDoor { get; private set; }

        public void Setup(PassengerColor color, Material material)
        {
            Color = color;
            reservedCount = 0;
            arrivedCount = 0;
            IsAtDoor = false;
            body.sharedMaterial = material;

            if (seatedPassengers == null)
                seatedPassengers = new Passenger[seatPoints.Length];
            else
                System.Array.Clear(seatedPassengers, 0, seatedPassengers.Length);

            for (int i = 0; i < seatIndicators.Length; i++)
                seatIndicators[i].SetActive(false);
        }

        public void SetAtDoor(bool value) => IsAtDoor = value;

        /// <summary>Bir koltuğu hemen rezerve eder; koltuğun noktasını ve indeksini döner.</summary>
        public Transform ReserveSeat(out int seatIndex)
        {
            seatIndex = reservedCount;
            reservedCount++;
            return seatPoints[seatIndex];
        }

        /// <summary>Yolcu koltuğa gerçekten vardı; gösterge dolar. Otobüs tam dolduysa true döner.</summary>
        public bool SeatArrived(int seatIndex, Passenger passenger)
        {
            seatedPassengers[seatIndex] = passenger;
            seatIndicators[seatIndex].SetActive(true);
            arrivedCount++;
            return arrivedCount >= seatPoints.Length;
        }

        /// <summary>Otobüs ayrılırken içindeki yolcuları BoardView'a geri vermek için; null olmayanlar doludur.</summary>
        public Passenger[] SeatedPassengers => seatedPassengers;
    }
}
