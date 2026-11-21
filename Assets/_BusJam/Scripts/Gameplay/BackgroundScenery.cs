using BusJam.Core;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Shows the seeded side scenery only in the landscape layout band.</summary>
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
