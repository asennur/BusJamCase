using BusJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    public sealed partial class ToolkitGameUI
    {
        private VisualElement hudTop, boosterBar, adBanner;

        /// <summary>Places the reference HUD: top column under the notch, banner on the bottom safe inset, booster bar
        /// above it. Columns are authored in reference pixels and scaled by screen height (ScreenLayout).</summary>
        private void LayoutHud(Rect safe, float width, float height)
        {
            if (hudTop == null || hudTop.panel != root.panel)
            { hudTop = root.Q("hud-top"); boosterBar = root.Q("booster-bar"); adBanner = root.Q("ad-banner"); }
            if (hudTop == null) return;
            float aspect = width / height;
            float unit = ScreenLayout.Unit(aspect) * height;
            float safeTop = height * (1f - safe.yMax), safeBottom = height * safe.yMin;
            var scale = new Scale(new Vector3(unit, unit, 1f));
            hudTop.style.top = ScreenLayout.HudTopFraction(safe, aspect) * height - safeTop;
            hudTop.style.scale = scale;
            boosterBar.style.bottom = (ScreenLayout.BannerHeight + ScreenLayout.BarGap) * unit;
            boosterBar.style.scale = scale;
            // The banner also covers the home-indicator strip below the safe area, as in the reference.
            adBanner.style.height = ScreenLayout.BannerHeight * unit + safeBottom;
            adBanner.style.bottom = -safeBottom;
            adBanner.style.paddingBottom = safeBottom;
        }
    }
}
