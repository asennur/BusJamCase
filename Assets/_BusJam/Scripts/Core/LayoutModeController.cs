using System;
using UnityEngine;

namespace BusJam.Core
{
    public enum LayoutMode { Portrait, Square, Landscape }

    /// <summary>UI düzenini değiştirmeden güvenli viewport ve en-boy bandı değişimlerini yayınlar.</summary>
    [ExecuteAlways]
    public sealed class LayoutModeController : MonoBehaviour
    {
        private int lastWidth, lastHeight;
        private Rect lastSafeArea;
#if UNITY_STANDALONE && !UNITY_EDITOR
        private static bool windowInitialized;
#endif
        public LayoutMode Mode { get; private set; }
        public Rect SafeViewport { get; private set; } = new Rect(0, 0, 1, 1);
        public event Action<LayoutMode> Changed;

        private void Awake()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            if (windowInitialized) return;
            windowInitialized = true;
            int height = Mathf.Min(960, Mathf.Max(320, Screen.currentResolution.height - 100));
            Screen.SetResolution(Mathf.RoundToInt(height * 9f / 16), height, FullScreenMode.Windowed);
#endif
        }

        private void OnEnable() => Refresh(true);
        private void Update() => Refresh(false);

        public static LayoutMode Classify(float aspect) => aspect < .8f ? LayoutMode.Portrait : aspect <= 1.25f ? LayoutMode.Square : LayoutMode.Landscape;

        public void Refresh(bool force = false)
        {
            int width = Screen.width, height = Screen.height;
            Rect safe = Screen.safeArea;
            if (width <= 0 || height <= 0) return;
            if (!force && width == lastWidth && height == lastHeight && safe == lastSafeArea) return;
            lastWidth = width; lastHeight = height; lastSafeArea = safe;
            // Editörde Device Simulator açıkken safeArea simüle edilen cihazın pikselleriyle gelir (Game view sekmesinde bile),
            // Screen ise görünümün boyutudur: güvenli alan oranla ekrana taşınır. Cihazda iki ölçü aynıdır.
            int deviceWidth = UnityEngine.Device.Screen.width, deviceHeight = UnityEngine.Device.Screen.height;
            if (deviceWidth > 0 && deviceHeight > 0 && (deviceWidth != width || deviceHeight != height))
            {
                float sx = (float)width / deviceWidth, sy = (float)height / deviceHeight;
                safe = new Rect(safe.x * sx, safe.y * sy, safe.width * sx, safe.height * sy);
            }
            ApplyViewport(width, height, safe);
        }

        /// <summary>Güvenli alanı (ekran pikseli) ekrana kırpıp normalize eder; geçersizse tam ekran sayar.</summary>
        public void ApplyViewport(int width, int height, Rect safe)
        {
            if (width <= 0 || height <= 0) return;
            safe = Rect.MinMaxRect(Mathf.Max(0, safe.xMin), Mathf.Max(0, safe.yMin), Mathf.Min(width, safe.xMax), Mathf.Min(height, safe.yMax));
            if (!(safe.width > 0 && safe.height > 0)) safe = new Rect(0, 0, width, height);
            SafeViewport = new Rect(safe.x / width, safe.y / height, safe.width / width, safe.height / height);
            Mode = Classify(safe.width / safe.height);
            Changed?.Invoke(Mode);
        }
    }
}
