using System.Collections.Generic;
using BusJam.Level;
using UnityEngine;
using UnityEngine.Pool;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Levelden karoları ve yolcuları havuzdan kurar, hücre → dünya dönüşümünü yapar. Kamera sahnede sabit.
    /// y = 0 satırı ekranın üstünde (otobüse yakın); grid -z yönünde uzar.
    /// Zemin (Empty/Passenger/Tunnel/Obstacle) çevresine köşe noktalarından modüler çerçeve dizer; engel hücresinde karo yerine bombe durur.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        public event System.Action<LevelData> Built;
        public float CellSize => cellSize;
        [SerializeField] private Passenger passengerPrefab;
        [SerializeField] private Transform tilePrefab;
        [SerializeField] private TunnelView tunnelPrefab;
        [Tooltip("Engel hücresinde karo yerine konan bombe (statik, toon).")]
        [SerializeField] private Transform obstaclePrefab;
        [Header("Çerçeve (köşe noktasına konur)")]
        [SerializeField] private Transform rimStraightPrefab;
        [SerializeField] private Transform rimOuterPrefab;
        [SerializeField] private Transform rimInnerPrefab;
        [Tooltip("Model ekseni farkı için parça başına sabit yaw (derece)")]
        [SerializeField] private float rimStraightYaw;
        [SerializeField] private float rimOuterYaw;
        [SerializeField] private float rimInnerYaw;
        [Tooltip("Kaldırım kenarı çizgisi tahtanın iki yanına bu kadar hücre uzar (ekranı kesecek kadar).")]
        [SerializeField, Min(0)] private int sidewalkEdgeExtraCells = 12;
        [Tooltip("Tahta tabanı çıkış kenarından kaldırıma doğru bu kadar hücre uzar; tahta ile kaldırım bandı arasında zemin şeridi görünmez.")]
        [SerializeField, Min(0)] private float sidewalkJoinDepth = 0.5f;
        [Tooltip("PassengerColor sırasıyla")]
        [SerializeField] private Material[] colorMaterials;
        [SerializeField] private Material hiddenMaterial;
        [Header("Erişilemez yolcu (renk başına bir kez türetilir)")]
        [Tooltip("Gövde parlaklığı çarpanı (HSV V); referans ~0.75-0.8.")]
        [SerializeField, Range(0f, 1f)] private float blockedBrightness = 0.8f;
        [Tooltip("Gövde doygunluğu çarpanı (HSV S).")]
        [SerializeField, Range(0f, 1f)] private float blockedSaturation = 0.95f;
        [Tooltip("Specular parlama çarpanı; referansta erişilemez yolcuda parlama yok.")]
        [SerializeField, Range(0f, 1f)] private float blockedSpecular = 0f;
        [Tooltip("İnce kontur kalınlığı, yolcu prefab'ındaki kalın konturun bu katı (Width ve Min px).")]
        [SerializeField, Range(0f, 1f)] private float thinOutlineScale = 0.4f;
        [Tooltip("İnce kontur rengi = gövde renginin parlaklığı × bu değer (aynı ton).")]
        [SerializeField, Range(0f, 1f)] private float thinOutlineBrightness = 0.4f;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Material baseMaterial;
        [Tooltip("Board backing and tile undersides only. Matches the surrounding sidewalk; the frame stays white.")]
        [SerializeField] private Material sidewalkMaterial;

        private Mesh baseMesh;
        private PassengerLook[] looks;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SpecStrengthId = Shader.PropertyToID("_SpecStrength");
        private static readonly int WidthId = Shader.PropertyToID("_Width");
        private static readonly int MinPixelsId = Shader.PropertyToID("_MinPixels");

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
            rimStraightPool = CreatePool(rimStraightPrefab);
            rimOuterPool = CreatePool(rimOuterPrefab);
            rimInnerPool = CreatePool(rimInnerPrefab);
            CreateLooks();
        }

        // Renk başına soluk gövde + koyu tonlu ince kontur materyali; hepsi aynı shader → SRP Batcher korunur.
        private void CreateLooks()
        {
            Material thick = passengerPrefab.OutlineMaterial;
            looks = new PassengerLook[colorMaterials.Length];
            for (int i = 0; i < colorMaterials.Length; i++)
            {
                Material source = colorMaterials[i];
                Color.RGBToHSV(source.GetColor(BaseColorId), out float h, out float s, out float v);

                var blocked = new Material(source) { name = source.name + " (Blocked)", hideFlags = HideFlags.DontSave };
                blocked.SetColor(BaseColorId, Color.HSVToRGB(h, s * blockedSaturation, v * blockedBrightness));
                blocked.SetFloat(SpecStrengthId, source.GetFloat(SpecStrengthId) * blockedSpecular);

                Material thin = null;
                if (thick != null)
                {
                    thin = new Material(thick) { name = source.name + " (Thin Outline)", hideFlags = HideFlags.DontSave };
                    thin.SetColor(BaseColorId, Color.HSVToRGB(h, s, v * thinOutlineBrightness));
                    thin.SetFloat(WidthId, thick.GetFloat(WidthId) * thinOutlineScale);
                    thin.SetFloat(MinPixelsId, thick.GetFloat(MinPixelsId) * thinOutlineScale);
                }
                looks[i] = new PassengerLook(source, blocked, thin);
            }
        }

        // Materyal override'ları havuzdaki board görsellerine aittir, asla paylaşılan kit prefab'larına ya da slotlara değil.
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

                    // Engel tahtanın içinde kalır (taban ve çerçeve onu da kapsar); karo yerine bombe.
                    if (data.type == CellType.Obstacle)
                    {
                        if (obstaclePrefab == null)
                            continue;
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
                    passenger.Setup(data.color, cell, looks[(int)data.color], data.hidden ? hiddenMaterial : null, data.cloak);
                    passenger.SetRopes(data.ropes);
                    activePassengers.Add(passenger);
                    board.SetOccupant(cell, passenger);
                }
            }

            BuildRim(level);
            BuildBase(level);
            Built?.Invoke(level);
        }

        // Tek mesh zemin hücrelerini ve çıkış kenarı boyunca kaldırıma bağlanan şeridi örter; Void açık kalır.
        // Shader yüksekliği kaldırıma düzlediği ve lekeleri dünya xz'de çizdiği için kaldırımla bindirme dikişsizdir.
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

            // Çıkış kenarı (z = +half) ile kaldırım bandı arasında ince zemin şeridi kalıyordu; kenar boyunca kaldırıma bindir.
            if (sidewalkJoinDepth > 0f)
            {
                float left = CellToLocal(new Vector2Int(-sidewalkEdgeExtraCells, 0)).x - half;
                float right = CellToLocal(new Vector2Int(level.width - 1 + sidewalkEdgeExtraCells, 0)).x + half;
                float near = half;
                float far = half + sidewalkJoinDepth * cellSize;
                int start = vertices.Count;
                vertices.Add(new Vector3(left, -0.09f, far));
                vertices.Add(new Vector3(right, -0.09f, far));
                vertices.Add(new Vector3(right, -0.09f, near));
                vertices.Add(new Vector3(left, -0.09f, near));
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }

            baseMesh.Clear();
            baseMesh.SetVertices(vertices);
            baseMesh.SetTriangles(triangles, 0);
            baseMesh.RecalculateNormals();
            baseMesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (looks != null)
                foreach (PassengerLook look in looks)
                {
                    DestroyAsset(look.Blocked);
                    DestroyAsset(look.ThinOutline);
                }
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

        private static void DestroyAsset(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }

        // Her köşe noktası 4 hücreye komşu: NE (x, y-1), SE (x, y), SW (x-1, y), NW (x-1, y-1).
        // Yaw 0'da Outer zemini NE'de, Inner boşluğu SW'de, Straight zemini kuzeyde (+z); her 90° saat yönünde bir çeyrek.
        private void BuildRim(LevelData level)
        {
            if (rimStraightPrefab == null)
                return;

            for (int cy = 0; cy <= level.height; cy++)
            {
                int extra = cy == 0 ? sidewalkEdgeExtraCells : 0;
                for (int cx = -extra; cx <= level.width + extra; cx++)
                {
                    // Çıkış satırının üstü kaldırım: zemin sayılır. Böylece kaldırım kenarı boyunca düz çerçeve,
                    // çıkış ağzının iki yanında kaldırıma kıvrılan iç köşe (fillet) oluşur.
                    bool ne = cy == 0 || IsGround(level, cx, cy - 1);
                    bool se = IsGround(level, cx, cy);
                    bool sw = IsGround(level, cx - 1, cy);
                    bool nw = cy == 0 || IsGround(level, cx - 1, cy - 1);
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
            return type == CellType.Empty || type == CellType.Passenger || type == CellType.Tunnel || type == CellType.Obstacle;
        }

        private static void PlaceRim(ObjectPool<Transform> pool, List<Transform> active, Vector3 local, float yaw)
        {
            Transform rim = pool.Get();
            rim.localPosition = local;
            rim.localRotation = Quaternion.Euler(0f, yaw, 0f);
            rim.localScale = Vector3.one;
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

        /// <summary>Rengin canlı materyali (level editörü önizlemesi).</summary>
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
            passenger.Setup(color, mouthCell, looks[(int)color]);
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
            // Zaten havuzdaki yolcu ikinci kez bırakılmaz; yoksa sonraki Get aynı nesneyi iki hücreye verir.
            if (activePassengers.Remove(passenger))
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
            activePassengers.Clear();
            activeTiles.Clear();
            activeTunnelViews.Clear();
            ReleaseAll(obstaclePool, activeObstacles);
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
