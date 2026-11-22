using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Oynanış arka plan shader'larının paylaştığı, ayarlanabilir tek palet işlemi.</summary>
    [CreateAssetMenu(menuName = "Bus Jam/Background Style")]
    public sealed class BackgroundStyle : ScriptableObject
    {
        [Tooltip("Reduces background colour intensity while keeping its original hue recognisable.")]
        [SerializeField, Range(0f, 1f)] private float desaturation = 0.25f;
        [Tooltip("Multiplies background brightness by one minus this value. 0.28 keeps 72% brightness.")]
        [SerializeField, Range(0f, 1f)] private float darken = 0.28f;
        [Tooltip("Gently compresses brighter tones without lifting black or adding a light haze.")]
        [SerializeField, Range(0f, 1f)] private float contrastReduction = 0.05f;

        private static readonly int ToneId = Shader.PropertyToID("_BusJamBackgroundTone");

        public void Apply()
        {
            Shader.SetGlobalVector(ToneId, new Vector4(desaturation, darken, contrastReduction, 0f));
        }

        private void OnValidate() => Apply();
    }
}
