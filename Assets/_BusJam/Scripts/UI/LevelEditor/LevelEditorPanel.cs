using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Level;
using BusJam.Save;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BusJam.LevelEditor
{
    /// <summary>
    /// Level editörü modeli + üst çubuk. Seçili levelin taslak kopyasını düzenler; asset yalnız Save'de
    /// ve yalnız taslak doğrulanıp solver çözüm bulursa yazılır. Solve/Save/Auto Fill solver'ı arka plan thread'inde
    /// taslağın kopyasıyla çalıştırır; Test Play kaydetmeden taslağın kopyasını oynatır. Board girdisi, palet, otobüs sırası çubuğu ve popup'lar bu sınıfı çağırır ve
    /// <see cref="Changed"/> ile yeniden çizer. Slotlar düzenlenmez: level numarasından gelir (GameRules).
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(UIDocument))]
    public partial class LevelEditorPanel : MonoBehaviour
    {
        public static readonly PassengerColor[] Colors = (PassengerColor[])Enum.GetValues(typeof(PassengerColor));

        [Header("Data and preview")]
        [SerializeField] private LevelDatabase database;
        [SerializeField] private BoardView boardView;
        [SerializeField] private Camera boardCamera;
        [Tooltip("Optional: shows the bus order on the road like in the game")]
        [SerializeField] private BusStation busStation;
        [Tooltip("Optional: shows the slot row for this level number like in the game")]
        [SerializeField] private WaitingArea waitingArea;
        [Tooltip("Flat translucent marker drawn on Void cells so the grid shape stays readable")]
        [SerializeField] private GameObject voidMarkerPrefab;
        [SerializeField] private string gameSceneName = "Game";

        private DropdownField levelList;
        private TextField width, height, seconds;
        private Label status, solverResult;

#if UNITY_EDITOR
        [Header("Solver")]
        [SerializeField, Min(1)] private int maxSolutionsShown = 10;
#endif

        public event Action Changed;
        public event Action<Vector2Int> TunnelSelected;

        public LevelData Draft => draft;
        /// <summary>Paletten seçilen renk; seçiliyken sol tık bu yolcuyu yerleştirir.</summary>
        public PassengerColor? SelectedColor { get; private set; }
        public int LevelNumber => sourceIndex >= 0 ? sourceIndex + 1 : database.Count + 1;

        private readonly Board board = new Board();
        private readonly List<GameObject> voidMarkers = new List<GameObject>();
        private LevelData source;
        private LevelData draft;
        private int sourceIndex = -1;
        private string lastMessage = "";
        // Taslağın her değişikliğinde artar; arka plan sonucu eski taslağa aitse yok sayılır.
        private int revision;
        // Test Play'e giderken taslak, level index'i ve kayıtlı anlık görüntü burada bekler.
        private static LevelData returnDraft;
        private static int returnIndex = -1;
        private static string returnSnapshot;

        private void Start()
        {
            if (!Application.isPlaying) return;
            if (returnDraft != null)
            {
                // Test Play'den dönüş: kaydedilmemiş değişiklikleriyle aynı taslak.
                var level = returnDraft;
                returnDraft = null;
                level.hideFlags = HideFlags.None;
                int index = returnIndex >= 0 && returnIndex < database.Count ? returnIndex : -1;
                sourceIndex = index;
                RefreshLevelList();
                Edit(level, index);
                savedSnapshot = returnSnapshot;
                SetStatus(HasUnsavedChanges ? "Back from Test Play. Your changes are kept, not saved yet." : "Back from Test Play.");
                return;
            }
            RefreshLevelList();
            if (database.Count > 0) LoadSelected();
            else NewLevel();
        }

        private void OnDestroy()
        {
            CancelJob();
            if (draft != null && draft != returnDraft) DestroyLevel(draft);
        }

        private static void DestroyLevel(LevelData level)
        {
            if (Application.isPlaying) Destroy(level);
            else DestroyImmediate(level);
        }

        private void RefreshLevelList()
        {
            var names = new List<string>(database.Count);
            for (int i = 0; i < database.Count; i++) names.Add($"Level {i + 1}  ({database.Get(i).name})");
            levelList.choices = names;
            if (names.Count > 0) levelList.SetValueWithoutNotify(names[Mathf.Clamp(sourceIndex, 0, names.Count - 1)]);
        }

        public void NewLevel()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.Resize(5, 6);
            level.timeLimit = 60f;
            Open(level, -1, true);
        }

        public void LoadSelected()
        {
            if (levelList.index >= 0 && levelList.index < database.Count) Open(database.Get(levelList.index), levelList.index, false);
        }

        private void Open(LevelData asset, int index, bool isNew)
        {
            Edit(isNew ? asset : Instantiate(asset), isNew ? -1 : index);
            SetStatus(isNew ? "New level. Set the size, add buses, then place passengers." : $"Loaded Level {LevelNumber}.");
            MarkSaved();
        }

        // Verilen taslağı düzenlemeye açar; index -1 kaydedilmemiş yeni level.
        private void Edit(LevelData level, int index)
        {
            CancelJob();
            if (draft != null && draft != level) DestroyLevel(draft);
            source = index >= 0 ? database.Get(index) : null;
            sourceIndex = index;
            SelectedColor = null;
            draft = level;
            width.SetValueWithoutNotify(draft.width.ToString());
            height.SetValueWithoutNotify(draft.height.ToString());
            seconds.SetValueWithoutNotify(draft.timeLimit.ToString("0", CultureInfo.InvariantCulture));
            // Yeni level listede yok; seçici onu adıyla gösterir.
            if (index < 0) levelList.SetValueWithoutNotify($"New level {LevelNumber} (not saved)");
            if (solverResult != null) solverResult.text = "";
            Rebuild(true);
        }

        // ---------- boyut / süre ----------

        public void ApplySize()
        {
            if (!int.TryParse(width.value, out int w) || !int.TryParse(height.value, out int h)
                || w < 1 || h < 1 || w > GameRules.MaxWidth || h > GameRules.MaxHeight)
            {
                SetStatus($"Size must be 1-{GameRules.MaxWidth} × 1-{GameRules.MaxHeight}.");
                return;
            }
            draft.Resize(w, h);
            draft.tunnels.RemoveAll(t => t.cell.x >= w || t.cell.y >= h);
            SetStatus($"Grid is {w} × {h}.");
            Rebuild(false);
        }

        public void ApplyTime()
        {
            if (float.TryParse(seconds.value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value >= 1f)
            {
                draft.timeLimit = Mathf.Round(value);
                SetStatus($"Time limit {draft.timeLimit:0} s.");
            }
            else
            {
                seconds.SetValueWithoutNotify(draft.timeLimit.ToString("0", CultureInfo.InvariantCulture));
                SetStatus("Time must be at least 1 second.");
            }
        }

        // ---------- hücreler ----------

        public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < draft.width && cell.y < draft.height;
        public CellData GetCell(Vector2Int cell) => draft.GetCell(cell.x, cell.y);

        public bool TryScreenToCell(Vector2 screen, out Vector2Int cell)
        {
            cell = default;
            return draft != null && boardView.TryPickCell(boardCamera.ScreenPointToRay(screen), out cell) && InBounds(cell);
        }

        /// <summary>Yolcu olmayan karoya tık: Tile → Void → Obstacle → Tunnel → Tile.</summary>
        public void CycleTile(Vector2Int cell)
        {
            var data = GetCell(cell);
            if (data.type == CellType.Passenger) return;
            CellType next = data.type switch
            {
                CellType.Empty => CellType.Void,
                CellType.Void => CellType.Obstacle,
                CellType.Obstacle => CellType.Tunnel,
                _ => CellType.Empty
            };
            draft.tunnels.RemoveAll(t => t.cell == cell);
            if (next == CellType.Tunnel)
                draft.tunnels.Add(new TunnelData { cell = cell, facing = Direction.Up });
            draft.SetCell(cell.x, cell.y, new CellData { type = next });
            SetStatus($"({cell.x}, {cell.y}) is now {Describe(next)}.");
            Rebuild(false);
            if (next == CellType.Tunnel) TunnelSelected?.Invoke(cell);
        }

        public TunnelData TunnelAt(Vector2Int cell) => draft.tunnels.Find(t => t.cell == cell);

        public void RotateTunnel(Vector2Int cell)
        {
            var tunnel = TunnelAt(cell);
            if (tunnel == null) return;
            tunnel.facing = tunnel.facing switch
            {
                Direction.Up => Direction.Right,
                Direction.Right => Direction.Down,
                Direction.Down => Direction.Left,
                _ => Direction.Up
            };
            SetStatus($"Tunnel at ({cell.x}, {cell.y}) faces {tunnel.facing}.");
            Rebuild(false);
        }

        public void AddToTunnel(Vector2Int cell, PassengerColor color)
        {
            var tunnel = TunnelAt(cell);
            if (tunnel == null || !CanAdd(color)) return;
            tunnel.queue.Add(color);
            Rebuild(false);
        }

        public void RemoveFromTunnel(Vector2Int cell, int index)
        {
            var tunnel = TunnelAt(cell);
            if (tunnel == null || index < 0 || index >= tunnel.queue.Count) return;
            tunnel.queue.RemoveAt(index);
            Rebuild(false);
        }

        public bool PlacePassenger(Vector2Int cell, PassengerColor color)
        {
            if (!InBounds(cell)) return false;
            var data = GetCell(cell);
            if (data.type != CellType.Empty && data.type != CellType.Passenger)
            {
                SetStatus($"Passengers go on tiles, not on a {Describe(data.type)}.");
                return false;
            }
            if (data.type == CellType.Passenger && data.color == color)
            {
                SetStatus($"There is already a {color} passenger here.");
                return false;
            }
            if (!CanAdd(color)) return false;
            draft.SetCell(cell.x, cell.y, new CellData { type = CellType.Passenger, color = color });
            SetStatus($"Placed {color} at ({cell.x}, {cell.y}).");
            if (Remaining(color) <= 0 && SelectedColor == color)
            {
                SelectedColor = null;
                SetStatus($"All {color} passengers are placed. Placing stopped.");
            }
            Rebuild(false);
            return true;
        }

        public bool MovePassenger(Vector2Int from, Vector2Int to)
        {
            if (from == to || !InBounds(to)) return false;
            var data = GetCell(from);
            if (data.type != CellType.Passenger || GetCell(to).type != CellType.Empty) return false;
            draft.SetCell(to.x, to.y, data);
            draft.SetCell(from.x, from.y, new CellData { type = CellType.Empty });
            Rebuild(false);
            return true;
        }

        public void RemovePassenger(Vector2Int cell)
        {
            if (GetCell(cell).type != CellType.Passenger) return;
            draft.SetCell(cell.x, cell.y, new CellData { type = CellType.Empty });
            Rebuild(false);
        }

        public void SetHidden(Vector2Int cell, bool value) => EditPassenger(cell, d => { d.hidden = value; if (value) d.cloak = Cloak.None; return d; });
        public void SetRopes(Vector2Int cell, int value) => EditPassenger(cell, d => { d.ropes = (byte)Mathf.Clamp(value, 0, 4); return d; });
        public void SetCloak(Vector2Int cell, Cloak value) => EditPassenger(cell, d => { d.cloak = value; if (value != Cloak.None) d.hidden = false; return d; });

        private void EditPassenger(Vector2Int cell, Func<CellData, CellData> edit)
        {
            var data = GetCell(cell);
            if (data.type != CellType.Passenger) return;
            draft.SetCell(cell.x, cell.y, edit(data));
            Rebuild(false);
        }

        // ---------- palet seçimi ----------

        public void ToggleSelection(PassengerColor color)
        {
            if (SelectedColor == color) { ClearSelection(); return; }
            if (Remaining(color) <= 0)
            {
                CanAdd(color);
                return;
            }
            SelectedColor = color;
            SetStatus($"Placing {color}: {Remaining(color)} left.");
            Changed?.Invoke();
        }

        public void ClearSelection()
        {
            if (!SelectedColor.HasValue) return;
            SelectedColor = null;
            SetStatus("Placing stopped.");
            Changed?.Invoke();
        }

        public void Hint(string text) => SetStatus(text);

        // ---------- otobüs sırası ----------

        public IReadOnlyList<PassengerColor> BusOrder => draft.busOrder;

        public void AddBus(PassengerColor color)
        {
            draft.busOrder.Add(color);
            Rebuild(true);
        }

        public void RemoveBusAt(int index)
        {
            if (index < 0 || index >= draft.busOrder.Count) return;
            draft.busOrder.RemoveAt(index);
            Rebuild(true);
        }

        public void MoveBus(int index, int delta)
        {
            int target = index + delta;
            if (index < 0 || target < 0 || index >= draft.busOrder.Count || target >= draft.busOrder.Count) return;
            (draft.busOrder[index], draft.busOrder[target]) = (draft.busOrder[target], draft.busOrder[index]);
            Rebuild(true);
        }

        // ---------- palet için sayılar ----------

        public int Required(PassengerColor color)
        {
            int buses = 0;
            foreach (var c in draft.busOrder) if (c == color) buses++;
            return buses * 3;
        }

        public int Placed(PassengerColor color)
        {
            int count = 0;
            foreach (var cell in draft.cells) if (cell.type == CellType.Passenger && cell.color == color) count++;
            foreach (var tunnel in draft.tunnels) foreach (var c in tunnel.queue) if (c == color) count++;
            return count;
        }

        /// <summary>Bu renkten yerleştirilecek kalan yolcular: otobüs başına 3, grid ve her tünel kuyruğu sayılarak.</summary>
        public int Remaining(PassengerColor color) => Required(color) - Placed(color);

        private bool CanAdd(PassengerColor color)
        {
            if (Remaining(color) > 0) return true;
            SetStatus(Required(color) == 0
                ? $"There is no {color} bus. Add a {color} bus first."
                : $"All {Required(color)} {color} passengers are placed. Add another {color} bus for more.");
            return false;
        }

        public Color DisplayColor(PassengerColor color)
        {
            var material = boardView.GetColorMaterial(color);
            if (material == null) return Color.white;
            return material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
        }

        // ---------- çöz / otomatik doldur / kaydet / test ----------

        public void Solve()
        {
#if UNITY_EDITOR
            if (Busy) return;
            if (!draft.Validate(out string error)) { ShowSolver($"Not valid yet: {error}"); return; }
            StartJob(Job.Solve);
            ShowSolver("Solving…");
#else
            ShowSolver("The solver runs in the Unity Editor.");
#endif
        }

        /// <summary>Girdilerdeki boyut için grid şeklini (delikler, engeller, bir tünel) üretir. Yolcuları temizler; otobüsleri ve süreyi korur.</summary>
        public void AutoGrid()
        {
            if (int.TryParse(width.value, out int w) && int.TryParse(height.value, out int h)
                && w >= 2 && h >= 2 && w <= GameRules.MaxWidth && h <= GameRules.MaxHeight
                && (w != draft.width || h != draft.height))
                draft.Resize(w, h);
            bool ok = LevelAutoGrid.Generate(draft, out string message);
            SelectedColor = null;
            ShowSolver(message);
            if (ok) Rebuild(false);
        }

        public void AutoFill()
        {
#if UNITY_EDITOR
            if (Busy) return;
            StartJob(Job.AutoFill);
            ShowSolver("Filling and solving…");
#else
            ShowSolver("Auto fill runs in the Unity Editor.");
#endif
        }

        /// <summary>Doğrular, sonra arka planda çözer; yalnız çözüm bulunursa asset'i yazar (yeni level listenin sonuna).</summary>
        public void Save()
        {
#if UNITY_EDITOR
            if (Busy) return;
            ApplyTime();
            if (draft.busOrder.Count == 0) { SetStatus("Cannot save: add at least one bus."); return; }
            if (!draft.Validate(out string error)) { SetStatus($"Cannot save: {error}"); return; }
            StartJob(Job.Save);
            ShowSolver("Solving…");
            SetStatus("Checking the level can be solved…");
#else
            SetStatus("Saving works in the Unity Editor.");
#endif
        }

        /// <summary>Kaydetmeden oyun sahnesinde taslağın kopyasını oynatır; dönüşte taslak aynen geri gelir.</summary>
        public void TestPlay()
        {
            ApplyTime();
            if (draft.busOrder.Count == 0) { SetStatus("Cannot test: add at least one bus."); return; }
            if (!draft.Validate(out string error)) { SetStatus($"Cannot test: {error}"); return; }
            CancelJob();
            var test = Instantiate(draft);
            test.name = source != null ? source.name : "Test level";
            draft.hideFlags = HideFlags.DontUnloadUnusedAsset;
            returnDraft = draft;
            returnIndex = sourceIndex;
            returnSnapshot = savedSnapshot;
            EditorReturn.Begin(SceneManager.GetActiveScene().name, test, LevelNumber);
            SceneManager.LoadScene(gameSceneName);
        }

#if UNITY_EDITOR
        // ---------- arka plan solver işi ----------

        private enum Job { None, Solve, Save, AutoFill }

        private sealed class JobResult
        {
            public LevelSolver.Result Solve;
            public bool Filled;
            public string Message;
        }

        private Job job;
        private Task<JobResult> task;
        private CancellationTokenSource cancel;
        private LevelData jobLevel;
        private int jobRevision;

        public bool Busy => task != null;

        // Taslağın kopyası ana thread'de alınır; thread yalnız bu kopyanın alanlarını okur/yazar, Unity API'sine dokunmaz.
        private void StartJob(Job kind)
        {
            CancelJob();
            job = kind;
            jobRevision = revision;
            jobLevel = Instantiate(draft);
            cancel = new CancellationTokenSource();
            var level = jobLevel;
            var token = cancel.Token;
            int number = LevelNumber, shown = maxSolutionsShown;
            task = kind switch
            {
                Job.Solve => Task.Run(() => new JobResult { Solve = LevelSolver.Solve(level, number, 400000, 6000, false, shown, token) }, token),
                Job.Save => Task.Run(() => new JobResult { Solve = LevelSolver.Solve(level, number, 400000, 6000, false, 1, token) }, token),
                _ => Task.Run(() => new JobResult { Filled = LevelAutoFill.Fill(level, number, out string message, 40, token), Message = message }, token)
            };
            SetBusy(true);
        }

        // Yeni iş, level değişimi ya da sahne kapanışı: çalışan iş iptal edilir, sonucu yok sayılır.
        private void CancelJob()
        {
            if (task == null) return;
            cancel.Cancel();
            cancel.Dispose();
            cancel = null;
            task = null;
            job = Job.None;
            if (jobLevel != null) DestroyLevel(jobLevel);
            jobLevel = null;
            SetBusy(false);
        }

        // Update'ten çağrılır: iş bittiyse sonucu ana thread'de uygular.
        private void PollJob()
        {
            if (task == null || !task.IsCompleted) return;
            var finished = task;
            var kind = job;
            var level = jobLevel;
            task = null; job = Job.None; jobLevel = null;
            cancel.Dispose(); cancel = null;
            SetBusy(false);
            try
            {
                if (finished.IsFaulted)
                {
                    Debug.LogException(finished.Exception.GetBaseException());
                    ShowSolver("The solver failed. See the Console.");
                    if (kind == Job.Save) SetStatus("Not saved: the solver failed.");
                    return;
                }
                if (finished.IsCanceled) return;
                if (jobRevision != revision)
                {
                    ShowSolver("The level changed while solving. Try again.");
                    if (kind == Job.Save) SetStatus("The level changed while checking. Not saved: press Save again.");
                    return;
                }
                var result = finished.Result;
                if (kind == Job.Solve) ShowSolveResult(result.Solve);
                else if (kind == Job.Save) FinishSave(result.Solve);
                else
                {
                    ShowSolver(result.Message);
                    if (!result.Filled) return;
                    draft.cells = level.cells;
                    draft.busOrder = level.busOrder;
                    draft.tunnels = level.tunnels;
                    Rebuild(true);
                }
            }
            finally
            {
                if (level != null) DestroyLevel(level);
            }
        }

        private void ShowSolveResult(LevelSolver.Result result)
        {
            if (result.SolutionCount == 0)
                ShowSolver(result.Limited ? "Search limit reached before a solution was found (too complex)." : "No solution. Players would always get stuck.");
            else
            {
                string count = result.SolutionCount >= maxSolutionsShown ? $"{maxSolutionsShown}+" : result.SolutionCount.ToString();
                string plural = result.SolutionCount == 1 ? "solution" : "solutions";
                ShowSolver($"{count} {plural} found.\nExample taps: {LevelSolver.Format(result.Solutions[0], draft.width)}");
            }
        }

        private void FinishSave(LevelSolver.Result result)
        {
            if (result.Error != null) { ShowSolver(result.Error); SetStatus($"Cannot save: {result.Error}"); return; }
            if (result.SolutionCount == 0)
            {
                string message = result.Limited ? "Could not verify solvable (too complex) — not saved." : "No solution — level not saved.";
                ShowSolver(message);
                return;
            }
            if (source == null)
            {
                string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                    $"Assets/_BusJam/Data/Levels/Level_{database.Count + 1:00}.asset");
                var asset = Instantiate(draft);
                asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
                UnityEditor.AssetDatabase.CreateAsset(asset, path);
                source = asset;
                database.Add(source);
                sourceIndex = database.Count - 1;
                UnityEditor.EditorUtility.SetDirty(database);
                RefreshLevelList();
                levelList.SetValueWithoutNotify(levelList.choices[sourceIndex]);
            }
            else
            {
                draft.name = source.name;
                UnityEditor.EditorUtility.CopySerialized(draft, source);
                UnityEditor.EditorUtility.SetDirty(source);
            }
            UnityEditor.AssetDatabase.SaveAssets();
            ShowSolver($"Solvable.\nExample taps: {LevelSolver.Format(result.Solutions[0], draft.width)}");
            SetStatus($"Saved as Level {LevelNumber} ({source.name}). The solver found a solution.");
            MarkSaved();
        }
#else
        public bool Busy => false;
        private void CancelJob() { }
        private void PollJob() { }
#endif

        // ---------- yeniden çizim ----------

        private void Rebuild(bool busesChanged)
        {
            revision++;
            board.Setup(draft);
            boardView.Build(draft, board);
            DrawVoidMarkers();
            if (busesChanged && busStation != null && draft.busOrder.Count > 0) busStation.Setup(draft.busOrder);
            if (waitingArea != null) waitingArea.Setup(LevelNumber);
            RenderStatus();
            Changed?.Invoke();
        }

        private void DrawVoidMarkers()
        {
            int used = 0;
            if (voidMarkerPrefab != null)
            {
                for (int y = 0; y < draft.height; y++)
                    for (int x = 0; x < draft.width; x++)
                    {
                        if (draft.GetCell(x, y).type != CellType.Void) continue;
                        if (used == voidMarkers.Count) voidMarkers.Add(Instantiate(voidMarkerPrefab, boardView.transform));
                        var marker = voidMarkers[used++];
                        marker.SetActive(true);
                        marker.transform.position = boardView.CellToWorld(new Vector2Int(x, y));
                    }
            }
            for (int i = used; i < voidMarkers.Count; i++) voidMarkers[i].SetActive(false);
        }

        private void SetStatus(string text)
        {
            lastMessage = text;
            RenderStatus();
        }

        // İlk satır: farenin şu an ne yaptığı. İkinci satır: son işlemin sonucu.
        private void RenderStatus()
        {
            if (status == null) return;
            string help = SelectedColor.HasValue
                ? $"<b>Placing {SelectedColor.Value}</b> ({Remaining(SelectedColor.Value)} left): tap tiles to place · tap {SelectedColor.Value} again or Esc to stop"
                : "<b>Tap a tile</b>: Tile > Hole > Obstacle > Tunnel · tap a tunnel or passenger to edit it · pick a colour on the left to place passengers";
            status.text = string.IsNullOrEmpty(lastMessage) ? help : $"{help}\n<color=#FFE066>{lastMessage}</color>";
        }

        // Solver/otomatik araç sonucu: tam metin sonuç kutusuna, ilk satırı durum satırına (eski mesaj kalmasın).
        private void ShowSolver(string text)
        {
            if (solverResult != null) solverResult.text = text;
            int line = text.IndexOf('\n');
            SetStatus(line < 0 ? text : text.Substring(0, line));
        }

        public static string Describe(CellType type) => type switch
        {
            CellType.Empty => "tile",
            CellType.Void => "hole (void)",
            CellType.Obstacle => "obstacle",
            CellType.Tunnel => "tunnel",
            _ => "passenger"
        };
    }
}
