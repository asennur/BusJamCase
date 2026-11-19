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

    /// <summary>Gridde bir yolcunun görünümü ve verisi. Karar vermez; ne yapacağını GameplayController söyler.</summary>
    public class Passenger : MonoBehaviour
    {
        [SerializeField] private Renderer body;
        [SerializeField] private GameObject outline;
        [SerializeField] private Transform angryIcon;
        [SerializeField] private GameObject hiddenMark;
        [SerializeField] private float angryDuration = 0.6f;

        [SerializeField, Min(0f)] private float strideHeight = 0.085f;
        [SerializeField, Min(0.01f)] private float halfStride = 0.105f;
        [SerializeField, Range(0f, 0.5f)] private float tapSquash = 0.16f;
        [SerializeField, Min(0.01f)] private float tapDuration = 0.18f;
        [SerializeField] private Transform waveArm;
        [SerializeField, Range(0f, 70f)] private float waveAngle = 32f;
        [SerializeField, Min(0.01f)] private float halfWave = 0.14f;

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
            body.sharedMaterial = material;
            outline.SetActive(false);
            angryIcon.gameObject.SetActive(false);
            hiddenMark.SetActive(hidden);
            runTween?.Kill();
            angryTween?.Kill();
            revealTween?.Kill();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            PendingSlotIndex = -1;
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
        public void SetInSlot() => State = PassengerState.InSlot;

        /// <summary>Slotlar doluyken taşıp WaitingArea'nın taşma kuyruğunda bekliyor; bir slot ayrılınca GameplayController onu oradan alır.</summary>
        public void SetIdle() => State = PassengerState.Idle;

        /// <summary>Otobüste koltuğa ışınlandı; koltuğa parent'lanır, otobüsle birlikte hareket eder.</summary>
        public void SetSeated(Transform seatPoint)
        {
            State = PassengerState.Seated;
            transform.SetParent(seatPoint, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Yol boyunca sabit hızla koşar (süre = uzunluk / hız), yöne dönerek. Varınca onArrived çağrılır.</summary>
        public void RunTo(List<Vector3> path, float speed, TweenCallback onArrived)
        {
            State = PassengerState.Running;
            SetReachable(false);

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
            strideTween = body.transform.DOLocalMoveY(bodyRestPosition.y + strideHeight, halfStride)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            runTween = transform.DOPath(path.ToArray(), duration, PathType.Linear)
                .SetEase(Ease.Linear)
                .SetLookAt(0.01f)
                .OnComplete(() =>
                {
                    strideTween.Kill();
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
