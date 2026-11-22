using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Görünen göstergeyi sabit oyun kamerasına hizalı tutar.</summary>
    public sealed class CameraFacingIcon : MonoBehaviour
    {
        private Transform view;
        private void OnEnable()
        {
            if (view == null && Camera.main != null) view = Camera.main.transform;
            FaceCamera();
        }
        private void LateUpdate() => FaceCamera();
        private void FaceCamera()
        {
            if (view != null) transform.rotation = view.rotation;
        }
    }
}
