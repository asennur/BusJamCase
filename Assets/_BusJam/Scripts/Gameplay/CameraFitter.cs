using BusJam.Core;
using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Dünyayı sabit FOV ile, tasarlanan bakış ekseni boyunca referans telefon kadrajına oturtur: otobüs tavanı HUD'un altında,
    /// grid altı alttaki boş şeridin üstünde, yüksekliğe göre ölçekli; tabletler yanlarda daha fazla zemin görür, referanstan (7×8) büyük
    /// gridler ya da dar ekranlar kamerayı geri çeker. Level editörü aynı kadrajı ekranın bir bölgesine (<see cref="SetScreenArea"/>) çizer.</summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class CameraFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayoutModeController layout;
        [Tooltip("Oyunda level yüklenince kadrajı yeniler; editörde boş, kadraj BoardView.Built ile yenilenir.")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BoardView board;
        [SerializeField] private Vector3 referencePosition;
        [SerializeField, Range(0, .1f)] private float padding = .015f;
        [Header("Reference layout (reference pixels, ScreenLayout)")]
        [SerializeField] private Vector3 busRoofLocal = new Vector3(0, 1.93f, 5.03f);
        [SerializeField] private float busRoofTop = 205f;
        [SerializeField] private float gridAboveBar = 12f;
        [SerializeField, Min(1)] private int referenceColumns = 7;
        [SerializeField, Min(1)] private int referenceRows = 8;
        [SerializeField, Min(0)] private float sideMargin = .12f;
        [SerializeField] private float slotRowLocal = 1.95f;
        private int columns, rows;
        private Vector2Int minimumGrid; // editör: kadraj level boyutundan bağımsız bu gride göre kurulur
        private Rect screenArea = new Rect(0, 0, 1, 1);
        public event System.Action Fitted;

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
            columns = level != null ? level.width : 0;
            rows = level != null ? level.height : 0;
            Fit();
        }

        /// <summary>Oyun ekranı yerine ekranın bu normalize bölgesine (ör. editörün board alanı) güvenli alansız bir telefon ekranı gibi çizer.</summary>
        public void SetScreenArea(Rect area)
        {
            if (!(area.width > 0 && area.height > 0)) area = new Rect(0, 0, 1, 1); // NaN (yerleşim öncesi) dahil
            if (area == screenArea) return;
            screenArea = area;
            Fit();
        }

        /// <summary>Kadrajı en az bu grid boyutuna göre kurar (editör 9×9 ile çağırır; 0×0 kapatır). Board yine ortada durur.</summary>
        public void SetMinimumGrid(int minColumns, int minRows)
        {
            var grid = new Vector2Int(minColumns, minRows);
            if (grid == minimumGrid) return;
            minimumGrid = grid;
            Fit();
        }

        /// <summary>Otobüs tavanı ve grid altı referans ekran yüksekliklerine otursun diye kamera uzaklığını ve dikey lens kaymasını
        /// çözer; grid ya da slotlar güvenli genişlikten genişse geri çeker. Ekran bölgesi verilmişse görüntü o bölgeye sıkıştırılır.</summary>
        [ContextMenu("Fit World")]
        public void Fit()
        {
            if (targetCamera == null || targetCamera.orthographic || board == null) return;
            bool fullScreen = screenArea == new Rect(0, 0, 1, 1);
            float aspect = targetCamera.aspect * screenArea.width / screenArea.height;
            if (!(aspect > 0 && aspect < 100f)) return; // pencere boyutu henüz 0 (Game view yeniden boyutlanırken)
            Rect safe = fullScreen && layout != null ? layout.SafeViewport : new Rect(0, 0, 1, 1);
            float unit = ScreenLayout.Unit(aspect);
            float hudShift = ScreenLayout.HudTopFraction(safe, aspect) - ScreenLayout.HudTop * unit;
            float top = 1f - busRoofTop * unit - hudShift;
            float bottom = ScreenLayout.BarTopFraction(safe, aspect) + gridAboveBar * unit;
            float cell = board.CellSize;
            int depth = Mathf.Max(Mathf.Max(rows, minimumGrid.y), referenceRows);
            Vector3 roof = board.transform.TransformPoint(busRoofLocal);
            Vector3 gridBottom = board.transform.TransformPoint(new Vector3(0, 0, (.5f - depth) * cell));
            float tanY = Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            float span = 2f * (top - bottom);
            // Tavandan gride izdüşen aralık uzaklıkla monoton olarak küçülür.
            float near = -10f, far = 500f;
            for (int i = 0; i < 40; i++)
            {
                float mid = (near + far) * .5f;
                if (Ndc(roof, mid, tanY) - Ndc(gridBottom, mid, tanY) > span) near = mid; else far = mid;
            }
            float distance = far;
            // Genişlik gerçek gridi kullanır; boş bir referans satır bandı kenardan taşabilir.
            float half = Mathf.Max(Mathf.Max(columns, minimumGrid.x), referenceColumns) * cell * .5f + sideMargin;
            int realDepth = Mathf.Max(rows > 0 ? rows : referenceRows, minimumGrid.y);
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
            if (!fullScreen)
            {
                // Sanal ekranın NDC'si bölgeye ölçeklenip kaydırılır (w = -z olduğundan kaydırma m02/m12'ye eklenir).
                projection.m00 *= screenArea.width; projection.m02 = projection.m02 * screenArea.width - (screenArea.xMin + screenArea.xMax - 1f);
                projection.m11 *= screenArea.height; projection.m12 = projection.m12 * screenArea.height - (screenArea.yMin + screenArea.yMax - 1f);
            }
            targetCamera.projectionMatrix = projection;
            Fitted?.Invoke();
        }

        // distance kadar geri çekilmiş bir kamera için dünya noktasının dikey NDC'si (lens kayması yok).
        private float Ndc(Vector3 world, float distance, float tanY)
        {
            Vector3 p = Quaternion.Inverse(transform.rotation) * (world - referencePosition + transform.forward * distance);
            return p.y / (p.z * tanY);
        }

        // Grid üstü, grid altı ve slot satırı üzerinde en geniş |x|/derinlik; tanX * kullanılabilir NDC yarı genişliği ile karşılaştırılır.
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
