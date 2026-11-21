using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Applies the referenced style on load and Inspector edits; no frame loop or material instances.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class BackgroundStyleBinding : MonoBehaviour
    {
        [SerializeField] private BackgroundStyle style;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();
        private void Apply()
        {
            if (style != null) style.Apply();
        }
    }
}
