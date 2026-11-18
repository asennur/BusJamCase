using System.Collections.Generic;
using BusJam.Level;
using UnityEngine;
using UnityEngine.Pool;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Levelden karoları ve yolcuları havuzdan kurar, hücre → dünya dönüşümünü yapar. Kamera sahnede sabit.
    /// y = 0 satırı ekranın üstünde (otobüse yakın); grid -z yönünde uzar.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private Passenger passengerPrefab;
        [SerializeField] private Transform tilePrefab;
        [SerializeField] private TunnelView tunnelPrefab;
        [SerializeField] private Transform obstaclePrefab;
        [Tooltip("PassengerColor sırasıyla")]
        [SerializeField] private Material[] colorMaterials;
        [SerializeField] private Material hiddenMaterial;
        [SerializeField] private float cellSize = 1f;

        private ObjectPool<Passenger> passengerPool;
        private ObjectPool<Transform> tilePool;
        private ObjectPool<TunnelView> tunnelPool;
        private ObjectPool<Transform> obstaclePool;
        private readonly List<Passenger> activePassengers = new List<Passenger>();
        private readonly List<Transform> activeTiles = new List<Transform>();
        private readonly List<TunnelView> activeTunnelViews = new List<TunnelView>();
        private readonly List<Transform> activeObstacles = new List<Transform>();
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

            tunnelPool = new ObjectPool<TunnelView>(
                () => Instantiate(tunnelPrefab, transform),
                t => t.gameObject.SetActive(true),
                t => t.gameObject.SetActive(false));

            obstaclePool = new ObjectPool<Transform>(
                () => Instantiate(obstaclePrefab, transform),
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

                    if (data.type == CellType.Tunnel)
                    {
                        TunnelData tunnelData = FindTunnelData(level, cell);
                        TunnelView tunnelView = tunnelPool.Get();
                        tunnelView.transform.localPosition = local;
                        tunnelView.Setup(cell, tunnelData.facing, tunnelData.queue.Count);
                        activeTunnelViews.Add(tunnelView);
                        continue;
                    }

                    Transform tile = tilePool.Get();
                    tile.localPosition = local;
                    activeTiles.Add(tile);

                    if (data.type == CellType.Obstacle)
                    {
                        Transform obstacle = obstaclePool.Get();
                        obstacle.localPosition = local;
                        activeObstacles.Add(obstacle);
                        continue;
                    }

                    if (data.type != CellType.Passenger)
                        continue;

                    Passenger passenger = passengerPool.Get();
                    passenger.transform.localPosition = local;
                    passenger.Setup(data.color, cell, data.hidden, data.hidden ? hiddenMaterial : colorMaterials[(int)data.color]);
                    activePassengers.Add(passenger);
                    board.SetOccupant(cell, passenger);
                }
            }
        }

        // Karo olmayan Void hücreleri de tahta düzleminde seçilebilir.
        public bool TryPickCell(Ray ray, out Vector2Int cell)
        {
            cell = default;
            var plane = new Plane(transform.up, transform.position);
            if (!plane.Raycast(ray, out float distance))
                return false;

            Vector3 local = transform.InverseTransformPoint(ray.GetPoint(distance));
            cell = new Vector2Int(Mathf.RoundToInt(local.x / cellSize + (width - 1) * 0.5f),
                Mathf.RoundToInt(-local.z / cellSize));
            return true;
        }

        public Vector3 CellToWorld(Vector2Int cell) => transform.TransformPoint(CellToLocal(cell));

        /// <summary>Gizli yolcu rengi açılırken kullanılacak gerçek renk materyali.</summary>
        public Material GetColorMaterial(PassengerColor color) => colorMaterials[(int)color];

        /// <summary>Kurulu tünel görünümlerinden hücresi eşleşeni döndürür (GameplayController kuyruk sayısını güncellemek için kullanır).</summary>
        public TunnelView GetTunnelView(Vector2Int tunnelCell)
        {
            for (int i = 0; i < activeTunnelViews.Count; i++)
                if (activeTunnelViews[i].Cell == tunnelCell)
                    return activeTunnelViews[i];

            return null;
        }

        /// <summary>Tünel ağzı boşalınca kuyruktan gelen yolcuyu havuzdan alır, tünelin konumundan ağız hücresine kaydırır.</summary>
        public Passenger SpawnFromTunnel(Vector2Int tunnelCell, Vector2Int mouthCell, PassengerColor color)
        {
            Passenger passenger = passengerPool.Get();
            passenger.transform.localPosition = CellToLocal(tunnelCell);
            passenger.Setup(color, mouthCell, false, colorMaterials[(int)color]);
            activePassengers.Add(passenger);
            passenger.EmergeFromTunnel(CellToLocal(mouthCell));
            return passenger;
        }

        private static TunnelData FindTunnelData(LevelData level, Vector2Int cell)
        {
            for (int i = 0; i < level.tunnels.Count; i++)
                if (level.tunnels[i].cell == cell)
                    return level.tunnels[i];

            return null;
        }

        /// <summary>Otobüsle giden ya da başka bir sebeple ayrılan tek yolcuyu havuza geri verir (parent BoardView'a döner).</summary>
        public void ReleasePassenger(Passenger passenger)
        {
            activePassengers.Remove(passenger);
            passengerPool.Release(passenger);
        }

        private Vector3 CellToLocal(Vector2Int cell) =>
            new Vector3((cell.x - (width - 1) * 0.5f) * cellSize, 0f, -cell.y * cellSize);

        private void Clear()
        {
            for (int i = 0; i < activePassengers.Count; i++)
                passengerPool.Release(activePassengers[i]);
            for (int i = 0; i < activeTiles.Count; i++)
                tilePool.Release(activeTiles[i]);
            for (int i = 0; i < activeTunnelViews.Count; i++)
                tunnelPool.Release(activeTunnelViews[i]);
            for (int i = 0; i < activeObstacles.Count; i++)
                obstaclePool.Release(activeObstacles[i]);
            activePassengers.Clear();
            activeTiles.Clear();
            activeTunnelViews.Clear();
            activeObstacles.Clear();
        }
    }
}
