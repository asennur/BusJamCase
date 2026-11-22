using BusJam.Level;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Tünelin görünümü: ağzı baktığı yöne döner, üstünde kalan yolcu sayısını gösterir.
    /// Sayı tünel dönse de prefab'daki dünya yönünde, yani kameraya dönük kalır. Yolcu çıkınca model kısa bir esneme yapar.</summary>
    public class TunnelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text remainingText;
        [SerializeField] private Transform badge;
        [SerializeField] private Transform model;
        [SerializeField, Range(0f, 0.3f)] private float emitPunch = 0.12f;
        [SerializeField, Min(0.01f)] private float emitDuration = 0.25f;

        private int lastRemaining = -1;
        private Quaternion badgeRotation;
        private Vector3 modelScale;
        private Tween punchTween;

        public Vector2Int Cell { get; private set; }

        private void Awake()
        {            badgeRotation = badge.rotation;
            modelScale = model.localScale;
        }

        private void OnDisable()
        {
            punchTween?.Kill();
            model.localScale = modelScale;
        }

        public void Setup(Vector2Int cell, Direction facing, int remaining)
        {
            Cell = cell;
            Vector2Int offset = facing.ToOffset();
            transform.localRotation = Quaternion.LookRotation(new Vector3(offset.x, 0f, -offset.y), Vector3.up);
            badge.rotation = badgeRotation;
            lastRemaining = -1;
            SetRemaining(remaining);
        }

        /// <summary>Sayıyı günceller; sayı azaldıysa (yolcu çıktı) modeli ağızdan dışarı doğru esnetir.</summary>
        public void SetRemaining(int remaining)
        {
            if (remaining == lastRemaining)
                return;

            bool emitted = lastRemaining >= 0 && remaining < lastRemaining;
            lastRemaining = remaining;
            remainingText.SetText("{0}", remaining);

            if (!emitted)
                return;
            punchTween?.Kill();
            model.localScale = modelScale;
            punchTween = model.DOPunchScale(new Vector3(emitPunch, -emitPunch, emitPunch), emitDuration, 1, 0.5f);
        }
    }
}
