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
        [Tooltip("Authored door-close clip duration; compressed into the anticipation.")]
        [SerializeField, Min(0f)] private float doorCloseDuration = 0.16666667f;
        [SerializeField] private Transform model;
        [SerializeField] private Transform[] wheels;
        [SerializeField, Min(0.01f)] private float wheelRadius = 0.24f;
        [SerializeField, Range(2f, 4f)] private float motionPitch = 3f;
        [SerializeField, Min(0f)] private float idleShakeHeight = 0.0012f;
        [SerializeField, Min(0f)] private float idleShakeRoll = 0.04f;
        [SerializeField, Range(4f, 15f)] private float idleShakeFrequency = 8f;

        private Material[] bodyMaterials;
        private int reservedCount;
        private int arrivedCount;
        private Passenger[] seatedPassengers;
        private Vector3 modelRestPosition;
        private Quaternion modelRestRotation;
        private Vector3 modelRestScale;
        private Quaternion[] wheelRestRotations;
        private Vector3 previousMotionPosition, arrivalPosition;
        private float wheelAngle;
        private float motionTime;
        private bool arriving, motionActive;
        private Sequence anticipation;
        private Passenger[] bufferedPassengers;
        private TweenCallback[] bufferedCallbacks;
        private bool idleShake = true;
        // Per-bus phase so queued buses do not shake in sync (reads as a screen shake otherwise).
        private float shakePhase;

        public PassengerColor Color { get; private set; }
        public bool IsReservedFull => reservedCount >= seatPoints.Length;
        public Transform DoorEntry => doorEntry;
        public bool CanApproach { get; private set; }
        public Vector3 ArrivalEntryPosition => doorEntry.position + arrivalPosition - transform.position;

        /// <summary>Allows boarding only after the bus reaches the door.</summary>
        public bool IsAtDoor { get; private set; }

        private void Awake()
        {
            bodyMaterials = body.sharedMaterials;
            seatedPassengers = new Passenger[seatPoints.Length];
            bufferedPassengers = new Passenger[seatPoints.Length];
            bufferedCallbacks = new TweenCallback[seatPoints.Length];
            wheelRestRotations = new Quaternion[wheels == null ? 0 : wheels.Length];
            for (int i = 0; i < wheelRestRotations.Length; i++)
                wheelRestRotations[i] = wheels[i].localRotation;
            if (model == null) return;
            modelRestPosition = model.localPosition;
            modelRestRotation = model.localRotation;
            modelRestScale = model.localScale;
            shakePhase = Random.value * Mathf.PI * 2f;
            anticipation = DOTween.Sequence()
                .Append(model.DOScale(Vector3.Scale(modelRestScale, new Vector3(1.025f, 0.95f, 1.025f)), 0.04f).SetEase(Ease.OutSine))
                .Append(model.DOScale(modelRestScale, 0.04f).SetEase(Ease.InSine))
                .SetAutoKill(false).Pause();
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
            transform.DOKill();
            anticipation?.Pause();
            motionActive = false;
            RestoreModel();
        }

        private void OnDestroy() => anticipation?.Kill();

        private void LateUpdate()
        {
            if (motionActive) { ApplyMotionPose(); return; }
            ApplyWheelPose();
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
            model.localScale = modelRestScale;
        }

        public void Setup(PassengerColor color, Material material)
        {
            Color = color;
            reservedCount = 0;
            arrivedCount = 0;
            IsAtDoor = false;
            CanApproach = false;
            motionActive = false;
            arrivalPosition = transform.position;
            wheelAngle = 0f;
            for (int i = 0; i < wheelRestRotations.Length; i++)
                wheels[i].localRotation = wheelRestRotations[i];
            System.Array.Clear(bufferedPassengers, 0, bufferedPassengers.Length);
            System.Array.Clear(bufferedCallbacks, 0, bufferedCallbacks.Length);
            DOTween.Kill(this);
            anticipation?.Pause();
            RestoreModel();
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

        public void SetAtDoor(bool value)
        {
            IsAtDoor = value;
            if (!value) { CanApproach = false; return; }
            CanApproach = true;
            for (int i = 0; i < bufferedPassengers.Length; i++)
            {
                if (bufferedPassengers[i] == null) continue;
                Passenger passenger = bufferedPassengers[i];
                TweenCallback callback = bufferedCallbacks[i];
                bufferedPassengers[i] = null;
                bufferedCallbacks[i] = null;
                passenger.EnterBus(doorEntry, seatPoints[i], callback);
            }
        }

        public void BoardWhenReady(int seatIndex, Passenger passenger, TweenCallback onSeated)
        {
            if (IsAtDoor) passenger.EnterBus(doorEntry, seatPoints[seatIndex], onSeated);
            else
            {
                bufferedPassengers[seatIndex] = passenger;
                bufferedCallbacks[seatIndex] = onSeated;
            }
        }

        public void AnticipateDeparture()
        {
            idleShake = false;
            RestoreModel();
            if (model == null) return;
            anticipation.Restart();
        }

        public void BeginMotion(Vector3 destination, bool isArrival)
        {
            PlayDrive();
            arrivalPosition = destination;
            arriving = isArrival;
            motionActive = true;
            motionTime = 0f;
            previousMotionPosition = transform.position;
        }

        public void SampleMotion(float time)
        {
            motionTime = time;
            wheelAngle += Vector3.Distance(previousMotionPosition, transform.position) / wheelRadius * Mathf.Rad2Deg;
            previousMotionPosition = transform.position;
            ApplyMotionPose();
        }

        private void ApplyMotionPose()
        {
            if (model != null)
            {
                float pitch = arriving ? -motionPitch * Mathf.Sin(Mathf.InverseLerp(0.45f, 1f, motionTime) * Mathf.PI)
                    : motionPitch * Mathf.Sin(motionTime * Mathf.PI);
                float bob = arriving && motionTime > 0.7f
                    ? Mathf.Sin((motionTime - 0.7f) / 0.3f * Mathf.PI * 2f) * 0.025f : 0f;
                model.localPosition = modelRestPosition + Vector3.up * bob;
                model.localRotation = Quaternion.AngleAxis(pitch, Vector3.forward) * modelRestRotation;
            }
            ApplyWheelPose();
        }

        private void ApplyWheelPose()
        {
            for (int i = 0; i < wheelRestRotations.Length; i++)
                wheels[i].localRotation = wheelRestRotations[i] * Quaternion.AngleAxis(-wheelAngle, Vector3.up);
        }

        public void AllowApproach()
        {
            CanApproach = true;
            PlayStop(true);
        }

        public void EndMotion()
        {
            SampleMotion(1f);
            motionActive = false;
            idleShake = true;
            RestoreModel();
        }

        /// <summary>Plays while advancing through the queue or departing.</summary>
        public void PlayDrive()
        {
            DOTween.Kill(this);
            Play(DriveHash);
        }

        /// <summary>Opens during the final settling motion; readiness still belongs to the station.</summary>
        public void PlayStop(bool atDoor, System.Action onReady = null)
        {
            DOTween.Kill(this);
            if (!atDoor || animator == null)
            {
                Play(IdleHash);
                onReady?.Invoke();
                return;
            }

            Play(DoorOpenHash);
            onReady?.Invoke();
        }

        /// <summary>Closes the full bus and returns the delay before departure.</summary>
        public float CloseDoor()
        {
            if (animator == null)
                return 0f;

            DOTween.Kill(this);
            Play(DoorCloseHash);
            animator.Play(DoorCloseHash, 0, 0f);
            animator.speed = doorCloseDuration / 0.08f;
            return 0.08f;
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
            {
                animator.speed = 1f;
                animator.CrossFadeInFixedTime(stateHash, crossFade);
            }
        }
    }
}
