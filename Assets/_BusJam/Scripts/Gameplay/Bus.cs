using BusJam.Level;
using DG.Tweening;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Tek otobüsün görünümü ve koltuk durumu. Rezervasyon (dokunma anında) ve varış (yolcu koltuğa
    /// ışınlanınca) ayrıdır: gösterge yalnız yolcu gerçekten varınca dolu görünür. Varan yolcular koltuğa
    /// parent'lanır (Passenger.SetSeated), bu yüzden otobüs onları kendileriyle birlikte taşır.
    /// Animator bağlıysa hareket, fren ve kapı klipleri koddan CrossFade ile oynar.
    /// </summary>
    public class Bus : MonoBehaviour
    {
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int DriveHash = Animator.StringToHash("Drive");
        private static readonly int BrakeHash = Animator.StringToHash("Brake");
        private static readonly int DoorOpenHash = Animator.StringToHash("DoorOpen");
        private static readonly int DoorCloseHash = Animator.StringToHash("DoorClose");

        [Tooltip("0. materyal yuvası otobüs rengidir")]
        [SerializeField] private Renderer body;
        [SerializeField] private Transform[] seatPoints;
        [SerializeField] private GameObject[] seatIndicators;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float crossFade = 0.1f;
        [SerializeField, Min(0f)] private float brakeDuration = 0.4f;
        [Tooltip("Kapı kapanma klibinin süresi; kalkış bu kadar bekler")]
        [SerializeField, Min(0f)] private float doorCloseDuration = 0.3f;

        private Material[] bodyMaterials;
        private int reservedCount;
        private int arrivedCount;
        private Passenger[] seatedPassengers;

        public PassengerColor Color { get; private set; }
        public bool IsReservedFull => reservedCount >= seatPoints.Length;

        /// <summary>Kuyruktan kapıya gelme tween'i bitince true olur; yolcular yalnız o zaman bu otobüse koşabilir.</summary>
        public bool IsAtDoor { get; private set; }

        private void Awake() => bodyMaterials = body.sharedMaterials;

        private void OnDisable() => DOTween.Kill(this);

        public void Setup(PassengerColor color, Material material)
        {
            Color = color;
            reservedCount = 0;
            arrivedCount = 0;
            IsAtDoor = false;
            DOTween.Kill(this);
            Play(IdleHash);

            bodyMaterials[0] = material;
            body.sharedMaterials = bodyMaterials;

            if (seatedPassengers == null)
                seatedPassengers = new Passenger[seatPoints.Length];
            else
                System.Array.Clear(seatedPassengers, 0, seatedPassengers.Length);

            if (seatIndicators != null)
                for (int i = 0; i < seatIndicators.Length; i++)
                    seatIndicators[i].SetActive(false);
        }

        public void SetAtDoor(bool value) => IsAtDoor = value;

        /// <summary>Kuyrukta kayarken ve ayrılırken.</summary>
        public void PlayDrive()
        {
            DOTween.Kill(this);
            Play(DriveHash);
        }

        /// <summary>Kuyrukta yerine vardı: kapıdaysa fren sonra Idle, değilse doğrudan Idle.</summary>
        public void PlayStop(bool atDoor)
        {
            DOTween.Kill(this);
            if (!atDoor || animator == null)
            {
                Play(IdleHash);
                return;
            }

            Play(BrakeHash);
            DOVirtual.DelayedCall(brakeDuration, () => Play(IdleHash)).SetTarget(this);
        }

        /// <summary>Dolunca kapıyı kapatır; kalkış için beklenecek süreyi döner.</summary>
        public float CloseDoor()
        {
            if (animator == null)
                return 0f;

            DOTween.Kill(this);
            Play(DoorCloseHash);
            return doorCloseDuration;
        }

        /// <summary>Bir koltuğu hemen rezerve eder; koltuğun noktasını ve indeksini döner. İlk rezervasyonda kapı açılır.</summary>
        public Transform ReserveSeat(out int seatIndex)
        {
            if (reservedCount == 0)
            {
                DOTween.Kill(this);
                Play(DoorOpenHash);
            }

            seatIndex = reservedCount;
            reservedCount++;
            return seatPoints[seatIndex];
        }

        /// <summary>Yolcu koltuğa gerçekten vardı; gösterge dolar. Otobüs tam dolduysa true döner.</summary>
        public bool SeatArrived(int seatIndex, Passenger passenger)
        {
            seatedPassengers[seatIndex] = passenger;
            if (seatIndicators != null && seatIndex < seatIndicators.Length)
                seatIndicators[seatIndex].SetActive(true);
            arrivedCount++;
            return arrivedCount >= seatPoints.Length;
        }

        /// <summary>Otobüs ayrılırken içindeki yolcuları BoardView'a geri vermek için; null olmayanlar doludur.</summary>
        public Passenger[] SeatedPassengers => seatedPassengers;

        private void Play(int stateHash)
        {
            if (animator != null)
                animator.CrossFadeInFixedTime(stateHash, crossFade);
        }
    }
}
