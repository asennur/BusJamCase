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

    /// <summary>Gridde bir yolcunun görünümü ve verisi. Karar vermez; ne yapacağını GameplayController söyler.
    /// Animator bağlıysa durumlar klip ile oynar; yoksa eski stride/kol tween'leri çalışır.</summary>
    public class Passenger : MonoBehaviour
    {
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int RunHash = Animator.StringToHash("Run");
        private static readonly int SitHash = Animator.StringToHash("Sit");
        private static readonly int WaveHash = Animator.StringToHash("Wave");

        [SerializeField] private Renderer body;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float crossFade = 0.1f;
        [SerializeField] private GameObject outline;
        [SerializeField] private Transform angryIcon;
        [SerializeField] private GameObject hiddenMark;
        [SerializeField] private GameObject[] ropeLayers;
        [SerializeField] private float angryDuration = 0.6f;

        [SerializeField, Min(0f)] private float strideHeight = 0.085f;
        [SerializeField, Min(0.01f)] private float halfStride = 0.105f;
        [SerializeField, Range(0f, 0.5f)] private float tapSquash = 0.16f;
        [SerializeField, Min(0.01f)] private float tapDuration = 0.18f;
        [SerializeField] private Transform waveArm;
        [SerializeField, Range(0f, 70f)] private float waveAngle = 32f;
        [SerializeField, Min(0.01f)] private float halfWave = 0.14f;
        [Tooltip("Koltuk kemiğine parent'lanınca yerel rotasyon düzeltmesi (model ekseni koltuktan farklıysa)")]
        [SerializeField] private Vector3 seatRotation;

        private Vector3 bodyRestPosition;
        private Tween strideTween;
        private Tween armTween;
        private Tween runTween;
        private Tween angryTween;
        private Tween revealTween;
        private Vector3 angryRestPosition;

        public PassengerColor Color { get; private set; }
        public Vector2Int Cell { get; private set; }
        public bool IsHidden { get; private set; }
        public int RopesLeft { get; private set; }
        public PassengerState State { get; private set; }

        /// <summary>Taşma kuyruğunda beklerken bir slot kendisine ayrıldıysa slot index'i; yoksa -1 (GameplayController kullanır).</summary>
        public int PendingSlotIndex { get; set; } = -1;

        private void Awake()
        {
            bodyRestPosition = body.transform.localPosition;
            angryRestPosition = angryIcon.localPosition;
        }

        private void OnDisable()
        {
            runTween?.Kill();
            angryTween?.Kill();
            revealTween?.Kill();
            strideTween?.Kill();
            armTween?.Kill();
            body.transform.localPosition = bodyRestPosition;
            if (waveArm != null)
                waveArm.gameObject.SetActive(false);
        }

        public void ShowTap()
        {
            revealTween?.Kill();
            transform.localScale = new Vector3(1f + tapSquash, 1f - tapSquash, 1f + tapSquash);
            revealTween = transform.DOScale(Vector3.one, tapDuration).SetEase(Ease.OutQuad);
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

        public void Setup(PassengerColor color, Vector2Int cell, bool hidden, Material material)
        {
            Color = color;
            Cell = cell;
            IsHidden = hidden;
            State = PassengerState.Idle;
            Play(IdleHash);
            body.sharedMaterial = material;
            body.enabled = true;
            outline.SetActive(false);
            angryIcon.gameObject.SetActive(false);
            hiddenMark.SetActive(hidden);
            runTween?.Kill();
            angryTween?.Kill();
            revealTween?.Kill();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            PendingSlotIndex = -1;
            SetRopes(0);
        }

        /// <summary>Havuzdan gelen yolcunun iplerini ve görünür katlarını sıfırlar.</summary>
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

        /// <summary>Bir kat ip çözülür; art arda çözülmelerde punch üst üste binmez.</summary>
        public void RemoveRope()
        {
            if (RopesLeft == 0)
                return;

            SetRopes(RopesLeft - 1);
            revealTween?.Kill();
            transform.localScale = Vector3.one;
            revealTween = transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 1, 0.5f);
        }

        /// <summary>Erişilebilir olan gizli yolcunun rengi açılır: gerçek materyal, "?" kalkar, kısa punch. Bir kez açılan geri gizlenmez.</summary>
        public void Reveal(Material material)
        {
            if (!IsHidden)
                return;

            IsHidden = false;
            body.sharedMaterial = material;
            hiddenMark.SetActive(false);

            revealTween?.Kill();
            revealTween = transform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 1, 0.5f);
        }

        /// <summary>Erişilebilir yolcu outline'lı görünür.</summary>
        public void SetReachable(bool reachable)
        {
            if (outline.activeSelf != reachable)
                outline.SetActive(reachable);
        }

        /// <summary>Bekleme slotuna vardı; otobüs gelince WaitingArea bunu otobüse koşturur.</summary>
        public void SetInSlot(Quaternion facing, float scale)
        {
            State = PassengerState.InSlot;
            transform.rotation = facing;
            transform.localScale = Vector3.one * scale;
            Play(IdleHash);
        }

        /// <summary>Slotlar doluyken taşıp WaitingArea'nın taşma kuyruğunda bekliyor; bir slot ayrılınca GameplayController onu oradan alır.</summary>
        public void SetIdle(Quaternion facing)
        {
            State = PassengerState.Idle;
            transform.rotation = facing;
            Play(IdleHash);
        }

        /// <summary>Otobüste koltuğa ışınlandı; koltuğa parent'lanır, otobüsle birlikte hareket eder.</summary>
        public void SetSeated(Transform seatPoint)
        {
            State = PassengerState.Seated;
            transform.SetParent(seatPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(seatRotation);
            Play(SitHash);
        }

        /// <summary>Kapıda küçülüp kaybolur, koltukta büyür; dolum yalnız belirme bitince bildirilir.</summary>
        public void EnterBus(Transform entry, Transform seatPoint, TweenCallback onSeated)
        {
            runTween?.Kill();
            Vector3 inside = entry.position + entry.forward * 0.18f;
            runTween = DOTween.Sequence()
                .Append(transform.DOMove(inside, 0.15f).SetEase(Ease.InQuad))
                .Join(transform.DOScale(0.05f, 0.15f).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    body.enabled = false;
                    SetSeated(seatPoint);
                })
                .AppendInterval(0.02f)
                .AppendCallback(() => body.enabled = true)
                .Append(transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
                .OnComplete(onSeated);
        }

        /// <summary>Yol boyunca sabit hızla koşar (süre = uzunluk / hız), yöne dönerek. Varınca onArrived çağrılır.</summary>
        public void RunTo(List<Vector3> path, float speed, TweenCallback onArrived)
        {
            State = PassengerState.Running;
            revealTween?.Kill();
            transform.localScale = Vector3.one;
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

        /// <summary>Tünelden ağız hücresine çıkış: kısa tween ile hedef local konuma kayar.</summary>
        public void EmergeFromTunnel(Vector3 localTarget)
        {
            runTween?.Kill();
            runTween = transform.DOLocalMove(localTarget, 0.3f).SetEase(Ease.OutQuad);
        }

        /// <summary>Taşma kuyruğunda beklerken öndeki biri ayrılınca bir adım öne kayar (kısa tween).</summary>
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

        /// <summary>Üstte kısa süre kırmızı ikon: zıplar, kaybolur. Art arda tıklamada tween yeniden başlar.</summary>
        public void ShowAngry()
        {
            angryTween?.Kill();
            angryIcon.gameObject.SetActive(true);
            angryIcon.localPosition = angryRestPosition;

            angryTween = angryIcon.DOLocalMoveY(angryRestPosition.y + 0.3f, angryDuration * 0.4f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => angryIcon.gameObject.SetActive(false));
        }
    }
}
