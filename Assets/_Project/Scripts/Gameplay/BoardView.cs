using System.Collections.Generic;
using BusJam.Level;
using UnityEngine;
using UnityEngine.Pool;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Levelden karoları ve yolcuları havuzdan kurar, hücre → dünya dönüşümünü yapar, kamerayı grid'e göre çerçeveler.
    /// y = 0 satırı ekranın üstünde (otobüse yakın); grid -z yönünde uzar.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private Passenger passengerPrefab;
        [SerializeField] private Transform tilePrefab;
        [Tooltip("PassengerColor sırasıyla")]
        [SerializeField] private Material[] colorMaterials;
        [SerializeField] private Material hiddenMaterial;
        [SerializeField] private Camera cam;
        [SerializeField] private float cellSize = 1f;
        [Tooltip("Grid'in üstünde bekleme alanı ve otobüs yolu için bırakılan boşluk")]
        [SerializeField] private float topSpace = 5f;
        [SerializeField] private float frameMargin = 0.6f;

        private ObjectPool<Passenger> passengerPool;
        private ObjectPool<Transform> tilePool;
        private readonly List<Passenger> activePassengers = new List<Passenger>();
        private readonly List<Transform> activeTiles = new List<Transform>();
        private int width;

        // GameManager.Awake, Board sahne sırasında daha önce gelse bile Build'den önce çalışabilir;
        // bu yüzden havuzlar Awake yerine ilk Build çağrısında kurulur.
        private void EnsurePools()
        {
            if (passengerPool != null)
                return;

            passengerPool = new ObjectPool<Passenger>(
                () => Instantiate(passengerPrefab, transform),
                p => p.gameObject.SetActive(true),
                p => { p.gameObject.SetActive(false); p.transform.SetParent(transform, false); });

            tilePool = new ObjectPool<Transform>(
                () => Instantiate(tilePrefab, transform),
                t => t.gameObject.SetActive(true),
                t => t.gameObject.SetActive(false));
        }

        /// <summary>Önceki leveli havuza geri verir, yenisini kurar ve yolcuları board'a yazar.</summary>
        public void Build(LevelData level, Board board)
        {
            EnsurePools();
            Clear();
            width = level.width;

            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    CellData data = level.GetCell(x, y);
                    if (data.type == CellType.Void)
                        continue;

                    var cell = new Vector2Int(x, y);
                    Vector3 local = CellToLocal(cell);

                    Transform tile = tilePool.Get();
                    tile.localPosition = local;
                    activeTiles.Add(tile);

                    if (data.type != CellType.Passenger)
                        continue;

                    Passenger passenger = passengerPool.Get();
                    passenger.transform.localPosition = local;
                    passenger.Setup(data.color, cell, data.hidden, data.hidden ? hiddenMaterial : colorMaterials[(int)data.color]);
                    activePassengers.Add(passenger);
                    board.SetOccupant(cell, passenger);
                }
            }

            FrameCamera(level.width, level.height);
        }

        public Vector3 CellToWorld(Vector2Int cell) => transform.TransformPoint(CellToLocal(cell));

        private Vector3 CellToLocal(Vector2Int cell) =>
            new Vector3((cell.x - (width - 1) * 0.5f) * cellSize, 0f, -cell.y * cellSize);

        private void Clear()
        {
            for (int i = 0; i < activePassengers.Count; i++)
                passengerPool.Release(activePassengers[i]);
            for (int i = 0; i < activeTiles.Count; i++)
                tilePool.Release(activeTiles[i]);
            activePassengers.Clear();
            activeTiles.Clear();
        }

        // Grid ve üstündeki boşluk ekrana sığacak kadar kamerayı bakış yönünde geri çeker; açı sahnedeki gibi kalır.
        // Eğik bakışta kameraya en yakın kenar (grid'in alt satırı) sınırı belirler; mesafe ona göre hesaplanır.
        private void FrameCamera(int gridWidth, int gridHeight)
        {
            float top = cellSize * 0.5f + topSpace;
            float bottom = -(gridHeight - 0.5f) * cellSize;
            Vector3 center = transform.TransformPoint(new Vector3(0f, 0f, (top + bottom) * 0.5f));

            float halfWidth = gridWidth * cellSize * 0.5f + frameMargin;
            float halfDepth = (top - bottom) * 0.5f + frameMargin;
            float pitch = cam.transform.eulerAngles.x * Mathf.Deg2Rad;
            float nearShift = halfDepth * Mathf.Cos(pitch);

            float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanH = tanV * cam.aspect;
            float distance = Mathf.Max(halfDepth * Mathf.Sin(pitch) / tanV, halfWidth / tanH) + nearShift;

            cam.transform.position = center - cam.transform.forward * distance;
        }
    }
}
