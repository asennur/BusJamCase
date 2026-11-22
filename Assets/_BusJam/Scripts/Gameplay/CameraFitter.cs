using BusJam.Core;
using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Frames the world along the authored view axis with a fixed FOV. In the game (GameManager bound) it follows
    /// the reference phone screenshots: bus roof under the HUD, grid bottom above the booster bar, height-scaled, so
    /// tablets only see more side ground; grids beyond the reference size or narrow screens pull the camera back.</summary>
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
        [Header("Reference layout (reference pixels, ScreenLayout)")]
        [SerializeField] private Vector3 busRoofLocal = new Vector3(0, 1.93f, 5.03f);
        [SerializeField] private float busRoofTop = 205f;
        [SerializeField] private float gridAboveBar = 12f;
        [SerializeField, Min(1)] private int referenceColumns = 7;
        [SerializeField, Min(1)] private int referenceRows = 8;
        [SerializeField, Min(0)] private float sideMargin = .12f;
        [SerializeField] private float slotRowLocal = 1.95f;
        private int columns, rows;
        private Bounds levelBounds;
        private bool hasLevel;
        public event System.Action Fitted;
        public Bounds CurrentBounds => hasLevel ? levelBounds : playBounds;

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
            columns = hasLevel ? level.width : 0; rows = hasLevel ? level.height : 0;
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
            if (gameManager != null && board != null) { FitReference(); return; }
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
    
        /// <summary>Solves dolly distance and vertical lens shift so the bus roof and grid bottom land on the reference
        /// screen heights; then pulls back if the grid or slots are wider than the safe width.</summary>
        private void FitReference()
        {
            float aspect = targetCamera.aspect;
            Rect safe = layout != null ? layout.SafeViewport : new Rect(0, 0, 1, 1);
            float unit = ScreenLayout.Unit(aspect);
            float hudShift = ScreenLayout.HudTopFraction(safe, aspect) - ScreenLayout.HudTop * unit;
            float top = 1f - busRoofTop * unit - hudShift;
            float bottom = ScreenLayout.BarTopFraction(safe, aspect) + gridAboveBar * unit;
            float cell = board.CellSize;
            int depth = Mathf.Max(rows, referenceRows);
            Vector3 roof = board.transform.TransformPoint(busRoofLocal);
            Vector3 gridBottom = board.transform.TransformPoint(new Vector3(0, 0, (.5f - depth) * cell));
            float tanY = Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            float span = 2f * (top - bottom);
            // The projected roof-to-grid span shrinks monotonically with distance.
            float near = -10f, far = 500f;
            for (int i = 0; i < 40; i++)
            {
                float mid = (near + far) * .5f;
                if (Ndc(roof, mid, tanY) - Ndc(gridBottom, mid, tanY) > span) near = mid; else far = mid;
            }
            float distance = far;
            // Width uses the real grid; an empty reference row band may run off the side.
            float half = Mathf.Max(columns, referenceColumns) * cell * .5f + sideMargin;
            int realDepth = rows > 0 ? rows : referenceRows;
            float limit = tanY * aspect * (safe.width - 2f * padding);
            if (Width(half, realDepth, cell, distance) > limit)
            {
                near = distance; far = 500f;
                for (int i = 0; i < 40; i++)
                {
                    float mid = (near + far) * .5f;
                    if (Width(half, realDepth, cell, mid) > limit) near = mid; else far = mid;
                }
                distance = far;
            }
            transform.position = referencePosition - transform.forward * distance;
            var projection = Matrix4x4.Perspective(targetCamera.fieldOfView, aspect, targetCamera.nearClipPlane, targetCamera.farClipPlane);
            projection.m02 = 1f - (safe.xMin + safe.xMax);
            projection.m12 = Ndc(roof, distance, tanY) - (2f * top - 1f);
            targetCamera.projectionMatrix = projection;
            Fitted?.Invoke();
        }

        // Vertical NDC (no lens shift) of a world point for a camera dollied back by distance.
        private float Ndc(Vector3 world, float distance, float tanY)
        {
            Vector3 p = Quaternion.Inverse(transform.rotation) * (world - referencePosition + transform.forward * distance);
            return p.y / (p.z * tanY);
        }

        // Widest |x|/depth over grid top, grid bottom and slot row; compared against tanX * usable NDC half-width.
        private float Width(float half, int depth, float cell, float distance)
        {
            float widest = 0;
            Quaternion inverse = Quaternion.Inverse(transform.rotation);
            for (int i = 0; i < 3; i++)
            {
                float z = i == 0 ? .5f * cell : i == 1 ? (.5f - depth) * cell : slotRowLocal;
                Vector3 p = inverse * (board.transform.TransformPoint(new Vector3(half, 0, z)) - referencePosition + transform.forward * distance);
                widest = Mathf.Max(widest, p.x / p.z);
            }
            return widest;
        }
    }
}
