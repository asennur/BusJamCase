using System.Collections.Generic;
using BusJam.Level;
using UnityEngine;
using UnityEngine.Pool;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Levelden karoları ve yolcuları havuzdan kurar, hücre → dünya dönüşümünü yapar. Kamera sahnede sabit.
    /// y = 0 satırı ekranın üstünde (otobüse yakın); grid -z yönünde uzar.
    /// Zemin (Empty/Passenger/Tunnel) çevresine köşe noktalarından modüler çerçeve dizer.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        public event System.Action<LevelData> Built;
        public float CellSize => cellSize;
        [SerializeField] private Passenger passengerPrefab;
        [SerializeField] private Transform tilePrefab;
        [SerializeField] private TunnelView tunnelPrefab;
        [Tooltip("Engel hücresi: karo yok, yalnız çukur (Kit_Hole)")]
        [SerializeField] private Transform obstaclePrefab;
        [Header("Çerçeve (köşe noktasına konur)")]
        [SerializeField] private Transform rimStraightPrefab;
        [SerializeField] private Transform rimOuterPrefab;
        [SerializeField] private Transform rimInnerPrefab;
        [Tooltip("Model ekseni farkı için parça başına sabit yaw (derece)")]
        [SerializeField] private float rimStraightYaw;
        [SerializeField] private float rimOuterYaw;
        [SerializeField] private float rimInnerYaw;
        [Tooltip("PassengerColor sırasıyla")]
        [SerializeField] private Material[] colorMaterials;
        [SerializeField] private Material hiddenMaterial;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Material baseMaterial;
        [Tooltip("Board-only backing and flush rim. Match the surrounding sidewalk colour and world height.")]
        [SerializeField] private Material sidewalkMaterial;

        private Mesh baseMesh;

        private ObjectPool<Passenger> passengerPool;
        private ObjectPool<Transform> tilePool;
        private ObjectPool<TunnelView> tunnelPool;
        private ObjectPool<Transform> obstaclePool;
        private readonly List<Passenger> activePassengers = new List<Passenger>();
        private readonly List<Transform> activeTiles = new List<Transform>();
        private readonly List<TunnelView> activeTunnelViews = new List<TunnelView>();
        private readonly List<Transform> activeObstacles = new List<Transform>();
        private ObjectPool<Transform> rimStraightPool;
        private ObjectPool<Transform> rimOuterPool;
        private ObjectPool<Transform> rimInnerPool;
        private readonly List<Transform> activeRimStraights = new List<Transform>();
        private readonly List<Transform> activeRimOuters = new List<Transform>();
        private readonly List<Transform> activeRimInners = new List<Transform>();
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
                () => CreateBoardVisual(tilePrefab, true),
                t => t.gameObject.SetActive(true),
                t => t.gameObject.SetActive(false));

            tunnelPool = new ObjectPool<TunnelView>(
                () => Instantiate(tunnelPrefab, transform),
                t => t.gameObject.SetActive(true),
                t => t.gameObject.SetActive(false));

            obstaclePool = CreatePool(obstaclePrefab);
            rimStraightPool = CreatePool(rimStraightPrefab, true);
            rimOuterPool = CreatePool(rimOuterPrefab, true);
            rimInnerPool = CreatePool(rimInnerPrefab, true);
        }

        // Material overrides belong to pooled board visuals, never the shared kit prefabs or slots.
        private Transform CreateBoardVisual(Transform prefab, bool tile)
        {
            Transform visual = Instantiate(prefab, transform);
            if (sidewalkMaterial == null) return visual;
            var renderer = visual.GetComponentInChildren<MeshRenderer>();
            if (tile)
            {
                var materials = renderer.sharedMaterials;
                materials[0] = sidewalkMaterial;
                renderer.sharedMaterials = materials;
            }
            else renderer.sharedMaterial = sidewalkMaterial;
            return visual;
        }

        private ObjectPool<Transform> CreatePool(Transform prefab, bool sidewalk = false) => new ObjectPool<Transform>(
            () => sidewalk ? CreateBoardVisual(prefab, false) : Instantiate(prefab, transform),
            t => t.gameObject.SetActive(true),
            t => t.gameObject.SetActive(false));

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

                    if (data.type == CellType.Obstacle)
                    {
                        Transform obstacle = obstaclePool.Get();
                        obstacle.localPosition = local;
                        activeObstacles.Add(obstacle);
                        continue;
                    }

                    Transform tile = tilePool.Get();
                    tile.localPosition = local;
                    activeTiles.Add(tile);

                    if (data.type == CellType.Tunnel)
                    {
                        TunnelData tunnelData = FindTunnelData(level, cell);
                        TunnelView tunnelView = tunnelPool.Get();
                        tunnelView.transform.localPosition = local;
                        tunnelView.Setup(cell, tunnelData.facing, tunnelData.queue.Count);
                        activeTunnelViews.Add(tunnelView);
                        continue;
                    }

                    if (data.type != CellType.Passenger)
                        continue;

                    Passenger passenger = passengerPool.Get();
                    passenger.transform.localPosition = local;
                    passenger.Setup(data.color, cell, data.hidden, data.hidden ? hiddenMaterial : colorMaterials[(int)data.color], data.cloak);
                    passenger.SetRopes(data.ropes);
                    activePassengers.Add(passenger);
                    board.SetOccupant(cell, passenger);
                }
            }

            BuildRim(level);
            BuildBase(level);
            Built?.Invoke(level);
        }

        // Tek mesh yalnız zemin hücrelerini örter; Void ve çukurlar açık kalır.
        private void BuildBase(LevelData level)
        {
            if (baseMesh == null)
            {
                var plate = new GameObject("BoardBase", typeof(MeshFilter), typeof(MeshRenderer));
                plate.transform.SetParent(transform, false);
                baseMesh = new Mesh { name = "BoardBase" };
                plate.GetComponent<MeshFilter>().sharedMesh = baseMesh;
                var renderer = plate.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = sidewalkMaterial != null ? sidewalkMaterial : baseMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float half = cellSize * 0.5f;
            for (int y = 0; y < level.height; y++)
            {
                for (int x = 0; x < level.width; x++)
                {
                    if (!IsGround(level, x, y))
                        continue;

                    Vector3 center = CellToLocal(new Vector2Int(x, y));
                    center.y = -0.09f;
                    int start = vertices.Count;
                    vertices.Add(center + new Vector3(-half, 0f, half));
                    vertices.Add(center + new Vector3(half, 0f, half));
                    vertices.Add(center + new Vector3(half, 0f, -half));
                    vertices.Add(center + new Vector3(-half, 0f, -half));
                    triangles.Add(start);
                    triangles.Add(start + 1);
                    triangles.Add(start + 2);
                    triangles.Add(start);
                    triangles.Add(start + 2);
                    triangles.Add(start + 3);
                }
            }

            baseMesh.Clear();
            baseMesh.SetVertices(vertices);
            baseMesh.SetTriangles(triangles, 0);
            baseMesh.RecalculateNormals();
            baseMesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (baseMesh != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(baseMesh);
                    return;
                }
#endif
                Destroy(baseMesh);
            }
        }

        // Her köşe noktası 4 hücreye komşu: NE (x, y-1), SE (x, y), SW (x-1, y), NW (x-1, y-1).
        // Yaw 0'da Outer zemini NE'de, Inner boşluğu SW'de, Straight zemini kuzeyde (+z); her 90° saat yönünde bir çeyrek.
        private void BuildRim(LevelData level)
        {
            if (rimStraightPrefab == null)
                return;

            for (int cy = 0; cy <= level.height; cy++)
            {
                for (int cx = 0; cx <= level.width; cx++)
                {
                    bool ne = IsGround(level, cx, cy - 1);
                    bool se = IsGround(level, cx, cy);
                    bool sw = IsGround(level, cx - 1, cy);
                    bool nw = IsGround(level, cx - 1, cy - 1);
                    int count = (ne ? 1 : 0) + (se ? 1 : 0) + (sw ? 1 : 0) + (nw ? 1 : 0);
                    if (count == 0 || count == 4)
                        continue;

                    Vector3 corner = CellToLocal(new Vector2Int(cx, cy)) + new Vector3(-0.5f, 0f, 0.5f) * cellSize;

                    if (count == 1)
                    {
                        int quadrant = ne ? 0 : se ? 1 : sw ? 2 : 3;
                        PlaceRim(rimOuterPool, activeRimOuters, corner, quadrant * 90f + rimOuterYaw);
                    }
                    else if (count == 3)
                    {
                        int empty = !ne ? 0 : !se ? 1 : !sw ? 2 : 3;
                        PlaceRim(rimInnerPool, activeRimInners, corner, (empty + 2) % 4 * 90f + rimInnerYaw);
                    }
                    else if (ne == sw)
                    {
                        // Çapraz iki zemin: iki ayrı dış köşe.
                        int first = ne ? 0 : 1;
                        PlaceRim(rimOuterPool, activeRimOuters, corner, first * 90f + rimOuterYaw);
                        PlaceRim(rimOuterPool, activeRimOuters, corner, (first + 2) * 90f + rimOuterYaw);
                    }
                    else
                    {
                        // Komşu iki zemin: kuzey 0, doğu 90, güney 180, batı 270.
                        int side = nw && ne ? 0 : ne && se ? 1 : se && sw ? 2 : 3;
                        PlaceRim(rimStraightPool, activeRimStraights, corner, side * 90f + rimStraightYaw);
                    }
                }
            }
        }

        private static bool IsGround(LevelData level, int x, int y)
        {
            if (x < 0 || y < 0 || x >= level.width || y >= level.height)
                return false;

            CellType type = level.GetCell(x, y).type;
            return type == CellType.Empty || type == CellType.Passenger || type == CellType.Tunnel;
        }

        private static void PlaceRim(ObjectPool<Transform> pool, List<Transform> active, Vector3 local, float yaw)
        {
            Transform rim = pool.Get();
            rim.localPosition = local;
            rim.localRotation = Quaternion.Euler(0f, yaw, 0f);
            active.Add(rim);
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
            ReleaseAll(rimStraightPool, activeRimStraights);
            ReleaseAll(rimOuterPool, activeRimOuters);
            ReleaseAll(rimInnerPool, activeRimInners);
        }

        private static void ReleaseAll(ObjectPool<Transform> pool, List<Transform> active)
        {
            for (int i = 0; i < active.Count; i++)
                pool.Release(active[i]);
            active.Clear();
        }
    }
}
