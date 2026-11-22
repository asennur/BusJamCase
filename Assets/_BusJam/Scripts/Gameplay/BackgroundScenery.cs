using BusJam.Core;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Tohumlu yan dekoru yalnız yatay düzen bandında gösterir.</summary>
    [ExecuteAlways]
    public sealed class BackgroundScenery : MonoBehaviour
    {
        [SerializeField] private LayoutModeController layout;
        [SerializeField] private GameObject wideScenery;

        private void OnEnable()
        {
            if (layout == null || wideScenery == null) return;
            layout.Changed += Apply;
            Apply(layout.Mode);
        }

        private void OnDisable()
        {
            if (layout != null) layout.Changed -= Apply;
        }

        private void Apply(LayoutMode mode) => wideScenery.SetActive(mode == LayoutMode.Landscape);
    }
}
