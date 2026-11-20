using System.Collections.Generic;
using BusJam.Level;
using DG.Tweening;
using UnityEngine;

namespace BusJam.Gameplay
{
    public enum PassengerState
    {
        Idle,
        Running,
        InSlot,
        Seated
    }

    /// <summary>Displays passenger state supplied by GameplayController, using clips or placeholder tweens.</summary>
    public class Passenger : MonoBehaviour
    {
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int RunHash = Animator.StringToHash("Run");
        private static readonly int SitHash = Animator.StringToHash("Sit");
        private static readonly int WaveHash = Animator.StringToHash("Wave");
        private static readonly int SlotIdleHash = Animator.StringToHash("SlotIdle");

        [SerializeField] private Renderer body;
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(1f)] private float standingVisualScale = 1.4f;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float crossFade = 0.1f;
        [SerializeField] private GameObject outline;
        [SerializeField] private Transform angryIcon;
        [SerializeField] private GameObject hiddenMark;
        [SerializeField] private GameObject[] ropeLayers;
        [SerializeField] private Transform cloakVisual;
        [SerializeField] private Material cloakMaterial;
        [SerializeField, Min(0.01f)] private float cloakDuration = 0.18f;
        [SerializeField] private float angryDuration = 0.6f;

        [SerializeField, Min(0f)] private float strideHeight = 0.085f;
        [SerializeField, Min(0.01f)] private float halfStride = 0.075f;
        [SerializeField, Range(0f, 0.5f)] private float tapSquash = 0.16f;
        [SerializeField, Min(0.01f)] private float tapDuration = 0.18f;
        [SerializeField] private Transform waveArm;
        [SerializeField, Range(0f, 70f)] private float waveAngle = 32f;
        [SerializeField, Min(0.01f)] private float halfWave = 0.14f;
        [Tooltip("Local rotation correction when parented to a seat bone.")]
        [SerializeField] private Vector3 seatRotation;

        private Vector3 bodyRestPosition;
        private Tween strideTween;
        private Tween armTween;
        private Tween runTween;
        private Tween angryTween;
        private Tween revealTween;
        private Vector3 angryRestPosition;
        private Vector3 angryRestScale;
        private Material colorMaterial;
        private Tween cloakTween;
        private bool isReachable;
        private Vector3 standingWorldScale;

        public PassengerColor Color { get; private set; }
        public Vector2Int Cell { get; private set; }
        public bool IsHidden { get; private set; }
        public int RopesLeft { get; private set; }
        public bool IsCloaked { get; private set; }
        public bool CloakOpen { get; private set; }
        public PassengerState State { get; private set; }

        /// <summary>Reserved waiting slot while in overflow, or -1 when none is reserved.</summary>
        public int PendingSlotIndex { get; set; } = -1;

        private void Awake()
        {
            bodyRestPosition = body.transform.localPosition;
            angryRestPosition = angryIcon.localPosition;
            angryRestScale = angryIcon.localScale;
        }

        private void OnDisable()
        {
            runTween?.Kill();
            angryTween?.Kill();
            revealTween?.Kill();
            strideTween?.Kill();
            armTween?.Kill();
            cloakTween?.Pause();
            body.transform.localPosition = bodyRestPosition;
            if (waveArm != null)
                waveArm.gameObject.SetActive(false);
        }

        public void ShowTap()
        {
            revealTween?.Kill();
            Vector3 restScale = LocalScaleFor();
            transform.localScale = Vector3.Scale(restScale, new Vector3(1f + tapSquash, 1f - tapSquash, 1f + tapSquash));
            revealTween = transform.DOScale(restScale, tapDuration).SetEase(Ease.OutQuad);
        }

        public void Wave()
        {
            if (animator != null)
            {
                Play(WaveHash);
                return;
            }

            if (waveArm == null)
                return;

            armTween?.Kill();
            waveArm.gameObject.SetActive(true);
            waveArm.localRotation = Quaternion.Euler(0f, 0f, -waveAngle);
            armTween = waveArm.DOLocalRotate(Vector3.forward * waveAngle, halfWave)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        public void Setup(PassengerColor color, Vector2Int cell, bool hidden, Material material, Cloak cloak = Cloak.None)
        {
            Color = color;
            Cell = cell;
            IsHidden = hidden;
            colorMaterial = material;
            IsCloaked = cloak != Cloak.None;
            CloakOpen = cloak != Cloak.StartsClosed;
            isReachable = false;
            State = PassengerState.Idle;
            if (visualRoot != null) visualRoot.localScale = Vector3.one * standingVisualScale;
            Play(IdleHash);
            body.sharedMaterial = CloakOpen ? material : cloakMaterial;
            body.enabled = true;
            outline.SetActive(false);
            angryIcon.gameObject.SetActive(false);
            hiddenMark.SetActive(hidden);
            runTween?.Kill();
            angryTween?.Kill();
            revealTween?.Kill();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            standingWorldScale = transform.lossyScale;
            PendingSlotIndex = -1;
            SetRopes(0);
            ResetCloakVisual();
        }

        // Each pooled passenger reuses one cloak tween.
        private void ResetCloakVisual()
        {
            if (cloakVisual == null)
                return;
            if (cloakTween == null && IsCloaked)
                cloakTween = cloakVisual.DOScale(Vector3.one, cloakDuration).From(Vector3.zero)
                    .SetEase(Ease.OutBack).SetAutoKill(false).OnRewind(HideOpenCloak).Pause();
            cloakTween?.Goto(CloakOpen ? 0f : cloakDuration, false);
            cloakVisual.gameObject.SetActive(IsCloaked && !CloakOpen);
        }

        /// <summary>Conceals the color with a reversible cloak tween.</summary>
        public void SetCloak(bool open)
        {
            if (!IsCloaked || CloakOpen == open)
                return;
            CloakOpen = open;
            body.sharedMaterial = open ? colorMaterial : cloakMaterial;
            SetReachable(isReachable);
            if (cloakVisual == null)
                return;
            cloakVisual.gameObject.SetActive(true);
            if (open)
            {
                cloakTween.PlayBackwards();
                if (cloakTween.position == 0f)
                    HideOpenCloak();
            }
            else
                cloakTween.PlayForward();
        }

        private void HideOpenCloak()
        {
            if (CloakOpen)
                cloakVisual.gameObject.SetActive(false);
        }

        private void OnDestroy() => cloakTween?.Kill();

        /// <summary>Resets rope count and visible layers when reusing a passenger.</summary>
        public void SetRopes(int count)
        {
            RopesLeft = Mathf.Clamp(count, 0, 4);
            if (ropeLayers != null)
                for (int i = 0; i < ropeLayers.Length; i++)
                    if (ropeLayers[i] != null)
                        ropeLayers[i].SetActive(i < RopesLeft);
            if (RopesLeft > 0)
                SetReachable(false);
        }

        /// <summary>Removes one rope and restarts the scale punch.</summary>
        public void RemoveRope()
        {
            if (RopesLeft == 0)
                return;

            SetRopes(RopesLeft - 1);
            revealTween?.Kill();
            transform.localScale = LocalScaleFor();
            revealTween = transform.DOPunchScale(LocalScaleFor(0.12f), 0.2f, 1, 0.5f);
        }

        /// <summary>Permanently reveals a hidden passenger's color and removes the question mark.</summary>
        public void Reveal(Material material)
        {
            if (!IsHidden)
                return;

            IsHidden = false;
            body.sharedMaterial = material;
            hiddenMark.SetActive(false);

            revealTween?.Kill();
            transform.localScale = LocalScaleFor();
            revealTween = transform.DOPunchScale(LocalScaleFor(0.25f), 0.3f, 1, 0.5f);
        }

        /// <summary>Shows the outline only when the passenger is reachable and unlocked.</summary>
        public void SetReachable(bool reachable)
        {
            isReachable = reachable;
            bool visible = reachable && RopesLeft == 0 && (!IsCloaked || CloakOpen);
            if (outline.activeSelf != visible)
                outline.SetActive(visible);
        }

        /// <summary>Waits in a slot until WaitingArea sends the passenger to a matching bus.</summary>
        public void SetInSlot(Quaternion facing)
        {
            State = PassengerState.InSlot;
            transform.rotation = facing;
            transform.localScale = LocalScaleFor();
            Play(WaitingHash);
        }

        /// <summary>Waits in overflow until GameplayController reserves a slot.</summary>
        public void SetIdle(Quaternion facing)
        {
            State = PassengerState.Idle;
            transform.rotation = facing;
            transform.localScale = LocalScaleFor();
            Play(WaitingHash);
        }

        // Slot and overflow waiting use the SlotIdle pose when the controller has it; grid passengers keep Idle.
        private int WaitingHash => animator != null && animator.HasState(0, SlotIdleHash) ? SlotIdleHash : IdleHash;

        /// <summary>Parents to the seat while retaining the grid's full world scale.</summary>
        public void SetSeated(Transform seatPoint)
        {
            State = PassengerState.Seated;
            if (visualRoot != null) visualRoot.localScale = Vector3.one * standingVisualScale;
            transform.SetParent(seatPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(seatRotation);
            transform.localScale = LocalScaleFor();
            Play(SitHash);
        }

        // Keep the size established on the grid, including under scaled seat bones.
        private Vector3 LocalScaleFor(float factor = 1f)
        {
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            return new Vector3(standingWorldScale.x * factor / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
                standingWorldScale.y * factor / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f),
                standingWorldScale.z * factor / Mathf.Max(Mathf.Abs(parentScale.z), 0.0001f));
        }

        /// <summary>Shrinks through the door, then grows to full seated size before reporting arrival.</summary>
        public void EnterBus(Transform entry, Transform seatPoint, TweenCallback onSeated)
        {
            runTween?.Kill();
            Vector3 inside = entry.position + entry.forward * 0.18f;
            runTween = DOTween.Sequence()
                .Append(transform.DOMove(inside, 0.15f).SetEase(Ease.InQuad))
                .Join(transform.DOScale(LocalScaleFor(0.05f), 0.15f).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    body.enabled = false;
                    SetSeated(seatPoint);
                    transform.localScale *= 0.05f;
                })
                .AppendInterval(0.02f)
                .AppendCallback(() => body.enabled = true)
                .Append(DOVirtual.Float(0.05f, 1f, 0.2f, scale =>
                {
                    transform.localScale = LocalScaleFor(scale);
                }).SetEase(Ease.OutBack))
                .OnComplete(onSeated);
        }

        /// <summary>Runs along the path at constant speed and reports arrival.</summary>
        public void RunTo(List<Vector3> path, float speed, TweenCallback onArrived)
        {
            State = PassengerState.Running;
            revealTween?.Kill();
            transform.localScale = LocalScaleFor();
            SetReachable(false);
            Play(RunHash);

            float length = 0f;
            Vector3 prev = transform.position;
            for (int i = 0; i < path.Count; i++)
            {
                length += Vector3.Distance(prev, path[i]);
                prev = path[i];
            }

            float duration = Mathf.Max(length / speed, 0.01f);

            runTween?.Kill();
            strideTween?.Kill();
            if (animator == null)
                strideTween = body.transform.DOLocalMoveY(bodyRestPosition.y + strideHeight, halfStride)
                    .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            runTween = transform.DOPath(path.ToArray(), duration, PathType.Linear)
                .SetEase(Ease.Linear)
                .SetLookAt(0.01f)
                .OnComplete(() =>
                {
                    strideTween?.Kill();
                    body.transform.localPosition = bodyRestPosition;
                    onArrived();
                });
        }

        /// <summary>Slides from the tunnel into its mouth cell.</summary>
        public void EmergeFromTunnel(Vector3 localTarget)
        {
            runTween?.Kill();
            runTween = transform.DOLocalMove(localTarget, 0.3f).SetEase(Ease.OutQuad);
        }

        /// <summary>Advances one place when someone leaves the overflow queue.</summary>
        public void ShiftOverflow(Vector3 worldTarget)
        {
            runTween?.Kill();
            runTween = transform.DOMove(worldTarget, 0.25f).SetEase(Ease.OutQuad);
        }

        private void Play(int stateHash)
        {
            if (animator != null)
                animator.CrossFadeInFixedTime(stateHash, crossFade);
        }

        /// <summary>Pops and bobs the angry icon, restarting on repeated taps.</summary>
        public void ShowAngry()
        {
            angryTween?.Kill();
            angryIcon.gameObject.SetActive(true);
            angryIcon.localPosition = angryRestPosition;
            angryIcon.localScale = Vector3.zero;

            angryTween = DOTween.Sequence()
                .Append(angryIcon.DOLocalMoveY(angryRestPosition.y + 0.3f, angryDuration * 0.4f)
                    .SetLoops(2, LoopType.Yoyo))
                .Insert(0f, angryIcon.DOScale(angryRestScale * 1.1f, angryDuration * 0.16f).SetEase(Ease.OutQuad))
                .Insert(angryDuration * 0.16f, angryIcon.DOScale(angryRestScale, angryDuration * 0.16f))
                .OnComplete(() => angryIcon.gameObject.SetActive(false));
        }
    }
}
