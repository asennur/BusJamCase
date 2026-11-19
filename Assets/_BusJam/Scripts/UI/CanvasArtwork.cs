using UnityEngine;
using UnityEngine.UI;

namespace BusJam.UI
{
    /// <summary>Envelopes a canvas rectangle without stretching art, including behind the safe area.</summary>
    [ExecuteAlways, RequireComponent(typeof(Image))]
    public sealed class CanvasArtwork : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Image image;
        [SerializeField] private Rect frame = new Rect(0, 0, 1, 1);
        private bool applying;

        private void OnEnable() => Fit();
        private void OnRectTransformDimensionsChange() => Fit();

        public void Fit()
        {
            if (applying || canvasRect == null || image == null || image.sprite == null) return;
            applying = true;
            try
            {
                var rect = (RectTransform)transform;
                var canvas = canvasRect.rect;
                var size = image.sprite.rect.size;
                float scale = Mathf.Max(canvas.width * frame.width / size.x, canvas.height * frame.height / size.y);
                Vector2 center = canvas.min + Vector2.Scale(canvas.size, frame.center);
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
                rect.position = canvasRect.TransformPoint(center);
                rect.sizeDelta = size * scale * canvasRect.lossyScale.x / rect.parent.lossyScale.x;
            }
            finally { applying = false; }
        }
    }
}
