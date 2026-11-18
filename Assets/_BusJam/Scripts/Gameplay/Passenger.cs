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
        [SerializeField] private float angryDuration = 0.6f;

        private Tween runTween;
        private Tween angryTween;
        private Vector3 angryRestPosition;

        public PassengerColor Color { get; private set; }
        public Vector2Int Cell { get; private set; }
        public bool IsHidden { get; private set; }
        public PassengerState State { get; private set; }

        private void Awake()
        {
            angryRestPosition = angryIcon.localPosition;
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
            runTween?.Kill();
            angryTween?.Kill();
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Erişilebilir yolcu outline'lı görünür.</summary>
        public void SetReachable(bool reachable)
        {
            if (outline.activeSelf != reachable)
                outline.SetActive(reachable);
        }

        /// <summary>Bekleme slotuna vardı; otobüs gelince WaitingArea bunu otobüse koşturur.</summary>
        public void SetInSlot() => State = PassengerState.InSlot;

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
            runTween = transform.DOPath(path.ToArray(), duration, PathType.Linear)
                .SetEase(Ease.Linear)
                .SetLookAt(0.01f)
                .OnComplete(onArrived);
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
