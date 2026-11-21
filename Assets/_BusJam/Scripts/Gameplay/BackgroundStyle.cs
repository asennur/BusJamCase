using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>One tunable palette treatment shared by the gameplay background shaders.</summary>
    [CreateAssetMenu(menuName = "Bus Jam/Background Style")]
    public sealed class BackgroundStyle : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float desaturation = 0.3f;
        [SerializeField, Range(0f, 1f)] private float lift = 0.2f;
        [SerializeField, Range(0f, 1f)] private float contrastReduction = 0.05f;
        [SerializeField] private Color haze = new Color(0.9f, 0.85f, 0.91f, 1f);

        private static readonly int ToneId = Shader.PropertyToID("_BusJamBackgroundTone");
        private static readonly int HazeId = Shader.PropertyToID("_BusJamBackgroundHaze");

        public void Apply()
        {
            Shader.SetGlobalVector(ToneId, new Vector4(desaturation, lift, contrastReduction, 0f));
            Color workingHaze = QualitySettings.activeColorSpace == ColorSpace.Linear ? haze.linear : haze;
            Shader.SetGlobalVector(HazeId, workingHaze);
        }

        private void OnValidate() => Apply();
    }
}
