using BusJam.Core;
using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Fits world bounds along the authored view axis; reference composition and FOV stay fixed.</summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class CameraFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayoutModeController layout;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BoardView board;
        [SerializeField] private Transform[] worldReferences;
        [SerializeField] private Bounds playBounds = new Bounds(new Vector3(0, 1, -2.625f), new Vector3(8.4f, 2.6f, 15.05f));
        [SerializeField] private Vector3 referencePosition;
        [SerializeField, Range(0, .1f)] private float padding = .015f;
        [SerializeField] private Rect freeViewport = new Rect(0, .045f, 1, .825f);
        [SerializeField, Min(0)] private float referenceHudBottomPixels = 310;
        private Bounds levelBounds;
        private bool hasLevel;
        public event System.Action Fitted;
        public Bounds CurrentBounds => hasLevel ? levelBounds : playBounds;
        public Vector3 ReferencePosition => referencePosition;

        private void OnEnable()
        {
            if (layout != null) layout.Changed += OnLayout;
            if (gameManager != null) gameManager.LevelLoaded += OnLevel;
            if (board != null) board.Built += SetLevel;
            if (targetCamera != null) Fit();
        }
        private void OnDisable()
        {
            if (layout != null) layout.Changed -= OnLayout;
            if (gameManager != null) gameManager.LevelLoaded -= OnLevel;
            if (board != null) board.Built -= SetLevel;
            if (targetCamera != null) targetCamera.ResetProjectionMatrix();
        }
        private void Start() => Fit();
        private void OnLayout(LayoutMode mode) => Fit();
        private void OnLevel(int number, LevelData level) => SetLevel(level);

        public void SetLevel(LevelData level)
        {
            levelBounds = playBounds; hasLevel = level != null && board != null;
            if (hasLevel)
            {
                float half = (level.width - 1) * board.CellSize * .5f;
                levelBounds.Encapsulate(board.transform.TransformPoint(new Vector3(-half - .65f, -.3f, -(level.height - 1) * board.CellSize - .65f)));
                levelBounds.Encapsulate(board.transform.TransformPoint(new Vector3(half + .65f, 1.8f, .65f)));
            }
            Fit();
        }

        public void Configure(Camera camera, LayoutModeController modes, GameManager manager, BoardView boardView, Transform[] references)
        {
            OnDisable(); targetCamera = camera; layout = modes; gameManager = manager; board = boardView;
            worldReferences = references; referencePosition = camera.transform.position; OnEnable();
        }

        [ContextMenu("Fit World")]
        public void Fit()
        {
            if (targetCamera == null || targetCamera.orthographic) return;
            Bounds bounds = CurrentBounds;
            if (worldReferences != null)
                for (int i = 0; i < worldReferences.Length; i++)
                    if (worldReferences[i] != null) bounds.Encapsulate(worldReferences[i].position);
            Rect safe = layout != null ? layout.SafeViewport : new Rect(0, 0, 1, 1);
            float left = Mathf.Max(safe.xMin, freeViewport.xMin) + padding;
            float right = Mathf.Min(safe.xMax, freeViewport.xMax) - padding;
            float bottom = Mathf.Max(safe.yMin, freeViewport.yMin) + padding;
            float top = Mathf.Min(safe.yMax, freeViewport.yMax) - padding;
            // Existing canvases use Match Width Or Height (.5); reserve their actual scaled top strip.
            if (targetCamera.aspect > .5626f)
            {
                float reservedHeight = referenceHudBottomPixels / 1920f * Mathf.Sqrt(targetCamera.aspect / .5625f);
                top = Mathf.Min(top, 1 - reservedHeight - padding);
            }
            float tanY = Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            float tanX = tanY * targetCamera.aspect;
            float centerY = targetCamera.aspect > .5626f ? (bottom + top) * .5f : .5f;
            float distance = 0;
            Vector3 min = bounds.min, max = bounds.max;
            Quaternion inverse = Quaternion.Inverse(transform.rotation);
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = inverse * (new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z) - referencePosition);
                float horizontal = p.x / (tanX * (p.x < 0 ? 2 * left - 1 : 2 * right - 1));
                float vertical = p.y / (tanY * 2 * (p.y < 0 ? bottom - centerY : top - centerY));
                distance = Mathf.Max(distance, Mathf.Max(horizontal, vertical) - p.z);
            }
            transform.position = referencePosition - transform.forward * distance;
            var projection = Matrix4x4.Perspective(targetCamera.fieldOfView, targetCamera.aspect, targetCamera.nearClipPlane, targetCamera.farClipPlane);
            projection.m12 = 1 - 2 * centerY;
            targetCamera.projectionMatrix = projection;
            Fitted?.Invoke();
        }
    }
}
