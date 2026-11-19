using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Extends the dim behind the safe area while content stays inside it.</summary>
    [ExecuteAlways]
    public sealed class FullscreenDim : MonoBehaviour
    {
        private void OnEnable() => Fit();
        private void OnRectTransformDimensionsChange() => Fit();
        private bool fitting;
        private void Fit()
        {
            if (fitting) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            fitting = true;
            var rect = (RectTransform)transform;
            var root = (RectTransform)canvas.transform;
            rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
            rect.position = root.TransformPoint(root.rect.center);
            rect.sizeDelta = root.rect.size * root.lossyScale.x / rect.parent.lossyScale.x;
            fitting = false;
        }
    }
}
