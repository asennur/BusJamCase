using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Extends a ground surface around the fitted frustum without moving its world-locked artwork.</summary>
    [ExecuteAlways]
    public sealed class WorldBackground : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private CameraFitter fitter;
        [SerializeField] private Transform ground;
        [SerializeField] private Transform laneAnchor;
        [SerializeField] private Transform roadCenterLine;
        [SerializeField] private Transform doorPoint;
        [SerializeField] private Transform stopMarker;
        public float LaneOffset => Mathf.Abs(roadCenterLine.position.z - laneAnchor.position.z);
        public float StopOffset => Vector2.Distance(new Vector2(stopMarker.position.x, stopMarker.position.z), new Vector2(doorPoint.position.x, doorPoint.position.z));
        private void OnEnable() { if (fitter != null) fitter.Fitted += Extend; Extend(); }
        private void OnDisable() { if (fitter != null) fitter.Fitted -= Extend; }

        public void Configure(Camera camera, CameraFitter cameraFitter, Transform surface, Transform lane, Transform centerLine, Transform door, Transform marker)
        {
            OnDisable(); worldCamera = camera; fitter = cameraFitter; ground = surface;
            laneAnchor = lane; roadCenterLine = centerLine; doorPoint = door; stopMarker = marker; OnEnable();
        }

        public void Extend()
        {
            if (worldCamera == null || ground == null) return;
            var plane = new Plane(Vector3.up, ground.position);
            float extent = 64;
            for (int i = 0; i < 4; i++)
            {
                Ray ray = worldCamera.ViewportPointToRay(new Vector3((i & 1) == 0 ? 0 : 1, (i & 2) == 0 ? 0 : 1));
                if (plane.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance) - ground.position;
                    extent = Mathf.Max(extent, Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.z)) + 8);
                }
            }
            ground.localScale = new Vector3(extent * 2, extent * 2, 1);
        }
    }
}
