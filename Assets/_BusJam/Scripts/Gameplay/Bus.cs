using BusJam.Level;
using DG.Tweening;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Displays one bus and tracks seat reservations separately from passenger arrivals.
    /// Controls drive, brake and door clips; seated passengers follow their seat bones.
    /// </summary>
    public class Bus : MonoBehaviour
    {
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int DriveHash = Animator.StringToHash("Drive");
        private static readonly int BrakeHash = Animator.StringToHash("Brake");
        private static readonly int DoorOpenHash = Animator.StringToHash("DoorOpen");
        private static readonly int DoorCloseHash = Animator.StringToHash("DoorClose");

        [Tooltip("Material slot 0 carries the bus color.")]
        [SerializeField] private Renderer body;
        [SerializeField] private Transform[] seatPoints;
        [SerializeField] private Transform doorEntry;
        [SerializeField] private GameObject[] seatIndicators;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float crossFade = 0.05f;
        [SerializeField, Min(0f)] private float brakeDuration = 0.3f;
        [Tooltip("Door-close duration to wait before departure.")]
        [SerializeField, Min(0f)] private float doorCloseDuration = 0.16666667f;
        [SerializeField] private Transform model;
        [SerializeField, Min(0f)] private float idleShakeHeight = 0.0012f;
        [SerializeField, Min(0f)] private float idleShakeRoll = 0.04f;
        [SerializeField, Range(4f, 15f)] private float idleShakeFrequency = 8f;

        private Material[] bodyMaterials;
        private int reservedCount;
        private int arrivedCount;
        private Passenger[] seatedPassengers;
        private Vector3 modelRestPosition;
        private Quaternion modelRestRotation;
        private bool idleShake = true;
        // Per-bus phase so queued buses do not shake in sync (reads as a screen shake otherwise).
        private float shakePhase;

        public PassengerColor Color { get; private set; }
        public bool IsReservedFull => reservedCount >= seatPoints.Length;
        public Transform DoorEntry => doorEntry;

        /// <summary>Allows boarding only after the bus reaches the door.</summary>
        public bool IsAtDoor { get; private set; }

        private void Awake()
        {
            bodyMaterials = body.sharedMaterials;
            if (model == null) return;
            modelRestPosition = model.localPosition;
            modelRestRotation = model.localRotation;
            shakePhase = Random.value * Mathf.PI * 2f;
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            RestoreModel();
        }

        private void LateUpdate()
        {
            if (!idleShake || model == null) return;
            float phase = Time.time * idleShakeFrequency * Mathf.PI * 2f + shakePhase;
            model.localPosition = modelRestPosition + Vector3.up * (Mathf.Sin(phase) * idleShakeHeight);
            model.localRotation = modelRestRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(phase + 0.8f) * idleShakeRoll);
        }

        private void RestoreModel()
        {
            if (model == null) return;
            model.localPosition = modelRestPosition;
            model.localRotation = modelRestRotation;
        }

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

        /// <summary>Plays while advancing through the queue or departing.</summary>
        public void PlayDrive()
        {
            DOTween.Kill(this);
            Play(DriveHash);
        }

        /// <summary>Opens the door after braking and holds its final pose until closing.</summary>
        public void PlayStop(bool atDoor, System.Action onReady = null)
        {
            DOTween.Kill(this);
            if (!atDoor || animator == null)
            {
                Play(IdleHash);
                onReady?.Invoke();
                return;
            }

            Play(BrakeHash);
            DOVirtual.DelayedCall(brakeDuration, () =>
            {
                Play(DoorOpenHash);
                onReady?.Invoke();
            }).SetTarget(this);
        }

        /// <summary>Closes the full bus and returns the delay before departure.</summary>
        public float CloseDoor()
        {
            if (animator == null)
                return 0f;

            DOTween.Kill(this);
            Play(DoorCloseHash);
            return doorCloseDuration;
        }

        /// <summary>Reserves a seat immediately without changing the door animation.</summary>
        public Transform ReserveSeat(out int seatIndex)
        {
            seatIndex = reservedCount;
            reservedCount++;
            return seatPoints[seatIndex];
        }

        /// <summary>Records an arrival and returns whether every seat is occupied.</summary>
        public bool SeatArrived(int seatIndex, Passenger passenger)
        {
            seatedPassengers[seatIndex] = passenger;
            if (seatIndicators != null && seatIndex < seatIndicators.Length)
                seatIndicators[seatIndex].SetActive(true);
            arrivedCount++;
            return arrivedCount >= seatPoints.Length;
        }

        /// <summary>Passengers to return to BoardView's pool after departure.</summary>
        public Passenger[] SeatedPassengers => seatedPassengers;

        private void Play(int stateHash)
        {
            idleShake = stateHash != BrakeHash && stateHash != DriveHash;
            if (!idleShake) RestoreModel();
            if (animator != null)
                animator.CrossFadeInFixedTime(stateHash, crossFade);
        }
    }
}
