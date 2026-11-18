using UnityEngine;

namespace BusJam.UI
{
    /// Paneli cihazın güvenli alanına (çentik, home bar dışı) sığdırır.
    /// Oyun portrait'e kilitli olduğundan yalnız açılışta uygulanır.
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private void Awake()
        {
            Rect safe = Screen.safeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            var rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
        }
    }
}
