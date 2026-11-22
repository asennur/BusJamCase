using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Bağlı stili yüklemede ve Inspector düzenlemelerinde uygular; kare döngüsü ya da materyal instance'ı yok.</summary>
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
