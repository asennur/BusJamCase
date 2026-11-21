using UnityEngine;

namespace BusJam.Core
{
    /// <summary>Reference phone layout (739×1600 capture) in reference pixels. UI and camera scale it by screen
    /// height; screens narrower than the reference shrink by width instead.</summary>
    public static class ScreenLayout
    {
        public const float ReferenceHeight = 1600f;
        public const float ReferenceAspect = 739f / 1600f;
        public const float ColumnWidth = 739f;
        public const float HudTop = 72f;       // Level card top edge
        public const float NotchGap = -17f;    // the reference card overlaps the status-bar strip by 17 px
        public const float BannerHeight = 93f; // ad banner placeholder, sits on the bottom safe inset
        public const float BarGap = 26f;       // booster bar bottom above the banner
        public const float BarHeight = 111f;   // locked booster buttons incl. labels

        /// <summary>Screen-height fraction of one reference pixel.</summary>
        public static float Unit(float aspect) => Mathf.Min(1f, aspect / ReferenceAspect) / ReferenceHeight;

        /// <summary>Top HUD edge as a screen-height fraction from the top; a deeper notch pushes it down (the reference card may overlap the status-bar strip by 17 px).</summary>
        public static float HudTopFraction(Rect safe, float aspect) =>
            Mathf.Max(HudTop * Unit(aspect), 1f - safe.yMax + NotchGap * Unit(aspect));

        /// <summary>Top edge of the booster bar as a screen-height fraction from the bottom.</summary>
        public static float BarTopFraction(Rect safe, float aspect) =>
            safe.yMin + (BannerHeight + BarGap + BarHeight) * Unit(aspect);
    }
}
