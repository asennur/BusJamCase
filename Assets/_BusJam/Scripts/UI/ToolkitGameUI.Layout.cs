using BusJam.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.UI
{
    public sealed partial class ToolkitGameUI
    {
        private VisualElement hudTop, barSpace, adBanner, resultColumn;
        private VisualElement[] popups, overlays;
        private VisualElement failBar;
        // Panel units per reference pixel; popups and the result column are authored in reference pixels.
        private float refScale = 1f;

        /// <summary>Places the reference HUD: top column under the notch, banner on the bottom safe inset, an empty
        /// bar strip above it (reserved space, confetti origin). Columns are authored in reference pixels and scaled by screen height (ScreenLayout).</summary>
        private void LayoutHud(Rect safe, float width, float height)
        {
            if (hudTop == null || hudTop.panel != root.panel)
            {
                hudTop = root.Q("hud-top"); barSpace = root.Q("bar-space"); adBanner = root.Q("ad-banner");
                resultColumn = root.Q("complete-panel");
                popups = new[] { settings.Q(className: "modal"), pause.Q(className: "modal"), pauseConfirmation.Q(className: "modal"), noLives.Q(className: "modal") };
                overlays = new[] { complete, fail, noLives, settings, pause, pauseConfirmation };
                failBar = fail.Q(className: "fail-bar");
            }
            if (hudTop == null) return;
            float aspect = width / height;
            float unit = ScreenLayout.Unit(aspect) * height;
            float safeTop = height * (1f - safe.yMax), safeBottom = height * safe.yMin;
            var scale = new Scale(new Vector3(unit, unit, 1f));
            hudTop.style.top = ScreenLayout.HudTopFraction(safe, aspect) * height - safeTop;
            hudTop.style.scale = scale;
            barSpace.style.bottom = (ScreenLayout.BannerHeight + ScreenLayout.BarGap) * unit;
            barSpace.style.scale = scale;
            // The banner also covers the home-indicator strip below the safe area, as in the reference.
            adBanner.style.height = ScreenLayout.BannerHeight * unit + safeBottom;
            adBanner.style.bottom = -safeBottom;
            adBanner.style.paddingBottom = safeBottom;
            LayoutPopups(unit, height, safeTop, safeBottom, width * safe.xMin, width * (1f - safe.xMax));
        }

        /// <summary>Scales popups and the result column like the reference: same share of screen height on phone
        /// and tablet. The result column (739x1600 reference pixels) is centred in the safe area.</summary>
        private void LayoutPopups(float unit, float height, float safeTop, float safeBottom, float safeLeft, float safeRight)
        {
            // Dims reach the screen edges (notch and home-indicator strips); padding keeps their content in the safe area.
            for (int i = 0; i < overlays.Length; i++)
            {
                var style = overlays[i].style;
                style.top = -safeTop; style.bottom = -safeBottom; style.left = -safeLeft; style.right = -safeRight;
                style.paddingTop = safeTop; style.paddingBottom = safeBottom; style.paddingLeft = safeLeft; style.paddingRight = safeRight;
            }
            failBar.style.top = safeTop;
            refScale = unit;
            var scale = new Scale(new Vector3(refScale, refScale, 1f));
            for (int i = 0; i < popups.Length; i++) if (popups[i] != popTarget || popupPop == null || !popupPop.IsPlaying()) popups[i].style.scale = scale;
            // A tall notch/home strip would push the coin plate onto the title: the column may shrink to the safe height
            // (3% overlap allowed, the reference status bar also overlaps the column edge).
            float safeHeight = height - safeTop - safeBottom;
            float column = Mathf.Min(refScale, safeHeight / (ScreenLayout.ReferenceHeight * .97f));
            resultColumn.style.scale = new Scale(new Vector3(column, column, 1f));
            resultColumn.style.top = safeTop + (safeHeight - ScreenLayout.ReferenceHeight * column) * .5f;
            if (shownState == GameState.Fail) SetFailureEntry(failureTime);
        }
    }
}
