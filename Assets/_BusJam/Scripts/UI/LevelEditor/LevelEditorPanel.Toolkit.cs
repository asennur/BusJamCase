using System;
using System.Collections.Generic;
using BusJam.Core;
using BusJam.Level;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.LevelEditor
{
    // Mevcut taslak editör API'si için UI Toolkit bağlantıları.
    public partial class LevelEditorPanel
    {
        private static readonly Direction[] Directions = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        [Header("UI Toolkit")]
        [SerializeField] private UIDocument document;
        [SerializeField] private LayoutModeController layoutMode;
        [SerializeField] private StyleSheet theme;
        private VisualElement panelRoot;
        private bool ownsPanelTheme;
        private VisualElement documentRoot, uiRoot, passengerOverlay, tunnelOverlay, ghost;
        private Label busSummary, passengerTitle, ropeLabel, tunnelTitle, tunnelCount;
        private Toggle hiddenPassenger;
        private DropdownField cloakList;
        private Button[] buttons, directionButtons, solverButtons;
        private Action[] actions;
        private PaletteEntry[] palette;
        private ChipRow buses, tunnelQueue;
        private Vector2Int passengerCell, tunnelCell;
        private bool bound;
        private bool waitingForTree;
        private PaletteEntry dragging;
        // Kaydedilmemiş değişiklik varken New/Load ikinci basışta uygulanır.
        private string savedSnapshot, pendingDiscard;

        public bool TunnelIsOpen => tunnelOverlay != null && !tunnelOverlay.ClassListContains("hidden");
        public Vector2Int OpenTunnelCell => tunnelCell;
        private bool HasUnsavedChanges => draft != null && JsonUtility.ToJson(draft) != savedSnapshot;

        private void OnEnable()
        {
            if (document == null || layoutMode == null || database == null) return;
            if (!CacheToolkit()) { waitingForTree = true; return; }
            waitingForTree = false;
            if (bound) return;
            bound = true;
            documentRoot.RegisterCallback<AttachToPanelEvent>(OnPanelAttached);
            ApplyPanelTheme(documentRoot.panel);
            for (int i = 0; i < buttons.Length; i++) buttons[i].clicked += actions[i];
            levelList.RegisterValueChangedCallback(OnLevelSelected);
            seconds.RegisterCallback<FocusOutEvent>(OnTimeChanged);
            hiddenPassenger.RegisterValueChangedCallback(OnHiddenChanged);
            cloakList.RegisterValueChangedCallback(OnCloakChanged);
            passengerOverlay.RegisterCallback<PointerDownEvent>(OnBackdrop);
            tunnelOverlay.RegisterCallback<PointerDownEvent>(OnBackdrop);
            uiRoot.RegisterCallback<GeometryChangedEvent>(OnEditorGeometry);
            layoutMode.Changed += ApplyEditorLayout;
            Changed += RefreshToolkit;
            TunnelSelected += ShowTunnel;
            foreach (var entry in palette) entry.Bind(true);
            buses.Bind(true); tunnelQueue.Bind(true);
            RefreshLevelList(); ApplyEditorLayout(layoutMode.Mode); RefreshToolkit();
        }

        private void OnDisable()
        {
            waitingForTree = false;
            if (!bound) return;
            bound = false; CancelDrag();
            documentRoot.UnregisterCallback<AttachToPanelEvent>(OnPanelAttached);
            if (ownsPanelTheme) panelRoot.styleSheets.Remove(theme);
            ownsPanelTheme = false; panelRoot = null;
            for (int i = 0; i < buttons.Length; i++) buttons[i].clicked -= actions[i];
            levelList.UnregisterValueChangedCallback(OnLevelSelected);
            seconds.UnregisterCallback<FocusOutEvent>(OnTimeChanged);
            hiddenPassenger.UnregisterValueChangedCallback(OnHiddenChanged);
            cloakList.UnregisterValueChangedCallback(OnCloakChanged);
            passengerOverlay.UnregisterCallback<PointerDownEvent>(OnBackdrop);
            tunnelOverlay.UnregisterCallback<PointerDownEvent>(OnBackdrop);
            uiRoot.UnregisterCallback<GeometryChangedEvent>(OnEditorGeometry);
            layoutMode.Changed -= ApplyEditorLayout;
            Changed -= RefreshToolkit; TunnelSelected -= ShowTunnel;
            foreach (var entry in palette) entry.Bind(false);
            buses.Bind(false); tunnelQueue.Bind(false);
        }

        private void Update()
        {
            if (waitingForTree && document.rootVisualElement?.Q("editor-root") != null) OnEnable();
            PollJob();
        }

        // UIDocument bu bileşenden sonra enable olabilir; yalnız dolu ağaç önbelleğe alınır.
        private bool CacheToolkit()
        {
            var current = document.rootVisualElement;
            if (current == null || current.Q("editor-root") == null) return false;
            if (documentRoot == current && uiRoot != null) return true;
            documentRoot = current;
            documentRoot.pickingMode = PickingMode.Ignore;
            uiRoot = documentRoot.Q("editor-root");
            BusJam.UI.VectorArt.Apply(uiRoot);
            levelList = uiRoot.Q<DropdownField>("level-list");
            width = uiRoot.Q<TextField>("width"); height = uiRoot.Q<TextField>("height"); seconds = uiRoot.Q<TextField>("seconds");
            status = uiRoot.Q<Label>("status"); solverResult = uiRoot.Q<Label>("solver-result"); busSummary = uiRoot.Q<Label>("bus-summary");
            passengerOverlay = uiRoot.Q("passenger-overlay"); tunnelOverlay = uiRoot.Q("tunnel-overlay");
            passengerTitle = uiRoot.Q<Label>("passenger-title"); ropeLabel = uiRoot.Q<Label>("ropes");
            tunnelTitle = uiRoot.Q<Label>("tunnel-title"); tunnelCount = uiRoot.Q<Label>("tunnel-count");
            hiddenPassenger = uiRoot.Q<Toggle>("hidden-passenger"); cloakList = uiRoot.Q<DropdownField>("cloak");
            cloakList.choices = new List<string> { "No cloak", "Starts open", "Starts closed" }; cloakList.index = 0;
            ghost = uiRoot.Q("passenger-ghost");
            string[] names = { "load", "new", "save", "test", "apply", "auto-grid", "solve", "auto-fill", "passenger-close", "ropes-minus", "ropes-plus",
                "delete-passenger", "tunnel-close", "delete-tunnel", "face-Up", "face-Down", "face-Left", "face-Right" };
            actions = new Action[] { RequestLoad, RequestNew, Save, TestPlay, ApplySize, AutoGrid, Solve, AutoFill, HidePassenger, RemoveRope, AddRope,
                DeletePassenger, HideTunnel, DeleteTunnel, () => FaceOpenTunnel(Direction.Up), () => FaceOpenTunnel(Direction.Down),
                () => FaceOpenTunnel(Direction.Left), () => FaceOpenTunnel(Direction.Right) };
            buttons = new Button[names.Length];
            for (int i = 0; i < names.Length; i++) buttons[i] = uiRoot.Q<Button>(names[i]);
            solverButtons = new[] { uiRoot.Q<Button>("save"), uiRoot.Q<Button>("solve"), uiRoot.Q<Button>("auto-fill") };
            directionButtons = new Button[Directions.Length];
            for (int i = 0; i < Directions.Length; i++) directionButtons[i] = uiRoot.Q<Button>("face-" + Directions[i]);
            buses = new ChipRow(this, uiRoot, "bus", AddBus, RemoveBusAt, MoveBus);
            tunnelQueue = new ChipRow(this, uiRoot, "tunnel", AddTunnelPassenger, RemoveTunnelPassenger, MoveTunnelPassenger);
            var container = uiRoot.Q<ScrollView>("palette");
            palette = new PaletteEntry[Colors.Length];
            for (int i = 0; i < Colors.Length; i++) { palette[i] = new PaletteEntry(this, Colors[i]); container.Add(palette[i].Element); }
            return true;
        }

        // Arka planda solver çalışırken Save/Solve/Auto Fill kilitlenir.
        private void SetBusy(bool busy)
        {
            if (solverButtons == null) return;
            foreach (var button in solverButtons) button.SetEnabled(!busy);
        }
        private void MarkSaved() { savedSnapshot = draft != null ? JsonUtility.ToJson(draft) : null; pendingDiscard = null; }

        // Kaydedilmemiş iş varsa ilk basış uyarır, aynı isteğin ikinci basışı uygular.
        private bool ConfirmDiscard(string request, string again)
        {
            if (!HasUnsavedChanges || pendingDiscard == request) { pendingDiscard = null; return true; }
            pendingDiscard = request;
            SetStatus($"Unsaved changes. {again} again to discard them, or Save first.");
            return false;
        }

        private void RequestNew()
        {
            if (!ConfirmDiscard("new", "Press New")) return;
            ClosePopups(); NewLevel();
        }

        private void RequestLoad()
        {
            if (levelList.index < 0) { SetStatus("Pick a level from the list first."); return; }
            if (!ConfirmDiscard("load:" + levelList.index, "Press Load")) return;
            ClosePopups(); LoadSelected();
        }

        // Yerel dropdown'lar panel kardeşidir, bu yüzden onlara da ortak stil sayfası gerekir.
        private void OnPanelAttached(AttachToPanelEvent evt) => ApplyPanelTheme(evt.destinationPanel);
        private void ApplyPanelTheme(IPanel panel)
        {
            if (panel == null || theme == null || ownsPanelTheme) return;
            panelRoot = panel.visualTree;
            if (panelRoot.styleSheets.Contains(theme)) return;
            panelRoot.styleSheets.Add(theme); ownsPanelTheme = true;
        }

        // Kaydedilmemiş değişiklik varsa seçim geri alınır; aynı level ikinci kez seçilince yüklenir.
        private void OnLevelSelected(ChangeEvent<string> evt)
        {
            int index = levelList.choices.IndexOf(evt.newValue);
            if (index < 0) return;
            if (!ConfirmDiscard("load:" + index, "Pick it")) { levelList.SetValueWithoutNotify(evt.previousValue); return; }
            ClosePopups(); LoadSelected();
        }

        private void OnTimeChanged(FocusOutEvent evt) { if (draft != null) ApplyTime(); }
        private void OnHiddenChanged(ChangeEvent<bool> evt)
        {
            bool hadCloak = GetCell(passengerCell).cloak != Cloak.None;
            SetHidden(passengerCell, evt.newValue);
            SetStatus(!evt.newValue ? "Hidden off." : hadCloak ? "Hidden on. The cloak was removed." : "Hidden on: the colour shows once the passenger can move.");
        }
        private void OnCloakChanged(ChangeEvent<string> evt)
        {
            bool wasHidden = GetCell(passengerCell).hidden;
            SetCloak(passengerCell, (Cloak)cloakList.index);
            SetStatus(cloakList.index == 0 ? "Cloak removed." : wasHidden ? "Cloak on. Hidden was turned off." : $"Cloak: {cloakList.value.ToLowerInvariant()}.");
        }
        private void RemoveRope()
        {
            int ropes = GetCell(passengerCell).ropes;
            if (ropes == 0) { SetStatus("No ropes to remove."); return; }
            SetRopes(passengerCell, ropes - 1);
        }
        private void AddRope()
        {
            int ropes = GetCell(passengerCell).ropes;
            if (ropes >= 4) { SetStatus("A passenger can have at most 4 ropes."); return; }
            SetRopes(passengerCell, ropes + 1);
            SetStatus("A rope comes off each time a neighbour leaves. Save checks that enough neighbours can leave.");
        }
        private void DeletePassenger()
        {
            RemovePassenger(passengerCell); HidePassenger();
            SetStatus($"Removed the passenger at ({passengerCell.x}, {passengerCell.y}).");
        }
        private void HidePassenger() => passengerOverlay.AddToClassList("hidden");
        private void HideTunnel() => tunnelOverlay.AddToClassList("hidden");
        private void AddTunnelPassenger(PassengerColor color) => AddToTunnel(tunnelCell, color);
        private void RemoveTunnelPassenger(int index) => RemoveFromTunnel(tunnelCell, index);

        /// <summary>Açık popup'ı kapatır; kapatacak bir şey yoksa false.</summary>
        public bool ClosePopups()
        {
            if (passengerOverlay == null) return false;
            bool open = TunnelIsOpen || !passengerOverlay.ClassListContains("hidden");
            HidePassenger(); HideTunnel();
            return open;
        }

        // Karartılmış arka plana dokunmak popup'ı kapatır (oyun popup'larıyla aynı).
        private void OnBackdrop(PointerDownEvent evt)
        {
            if (evt.target != evt.currentTarget) return;
            ClosePopups(); evt.StopPropagation();
        }

        private void FaceOpenTunnel(Direction facing)
        {
            var tunnel = TunnelAt(tunnelCell);
            if (tunnel == null || tunnel.facing == facing) return;
            if (ExitHasPassenger(tunnelCell, facing))
            {
                var mouth = tunnelCell + facing.ToOffset();
                SetStatus($"{ExitMustStayEmpty} Move the passenger at ({mouth.x}, {mouth.y}) first.");
                return;
            }
            tunnel.facing = facing;
            SetStatus(InBounds(tunnel.MouthCell) ? $"Tunnel faces {facing}." : $"Tunnel faces {facing}, off the grid. Turn it towards a tile or Save will fail.");
            Rebuild(false);
        }

        private void MoveTunnelPassenger(int index, int delta)
        {
            var tunnel = TunnelAt(tunnelCell);
            int target = index + delta;
            if (tunnel == null || index < 0 || target < 0 || index >= tunnel.queue.Count || target >= tunnel.queue.Count) return;
            (tunnel.queue[index], tunnel.queue[target]) = (tunnel.queue[target], tunnel.queue[index]);
            Rebuild(false);
        }

        private void DeleteTunnel()
        {
            if (TunnelAt(tunnelCell) == null) return;
            draft.tunnels.RemoveAll(t => t.cell == tunnelCell);
            draft.SetCell(tunnelCell.x, tunnelCell.y, new CellData { type = CellType.Empty });
            HideTunnel();
            SetStatus($"Removed the tunnel at ({tunnelCell.x}, {tunnelCell.y}). Its passengers can be placed again.");
            Rebuild(false);
        }

        public void ShowPassenger(Vector2Int cell)
        {
            passengerCell = cell; HideTunnel(); passengerOverlay.RemoveFromClassList("hidden"); RefreshToolkit();
        }

        public void ShowTunnel(Vector2Int cell)
        {
            tunnelCell = cell; HidePassenger(); tunnelQueue.Select(-1); tunnelOverlay.RemoveFromClassList("hidden"); RefreshToolkit();
        }

        private void RefreshToolkit()
        {
            if (draft == null || uiRoot == null) return;
            buses.Show(BusOrder);
            busSummary.text = BusOrder.Count == 0 ? "Buses: add at least one" : $"Buses: {BusOrder.Count}  ·  passengers needed: {BusOrder.Count * 3}";
            foreach (var entry in palette) entry.Refresh();
            if (!passengerOverlay.ClassListContains("hidden"))
            {
                if (!InBounds(passengerCell) || GetCell(passengerCell).type != CellType.Passenger) HidePassenger();
                else
                {
                    var data = GetCell(passengerCell);
                    passengerTitle.text = $"{data.color} ({passengerCell.x}, {passengerCell.y})";
                    hiddenPassenger.SetValueWithoutNotify(data.hidden);
                    ropeLabel.text = data.ropes.ToString(); cloakList.SetValueWithoutNotify(cloakList.choices[(int)data.cloak]);
                }
            }
            if (TunnelIsOpen)
            {
                var tunnel = TunnelAt(tunnelCell);
                if (tunnel == null) HideTunnel();
                else
                {
                    tunnelTitle.text = $"Tunnel ({tunnelCell.x}, {tunnelCell.y})";
                    tunnelCount.text = tunnel.queue.Count == 0 ? "Queue: empty" : $"Queue: {tunnel.queue.Count} inside";
                    for (int i = 0; i < Directions.Length; i++) SetButtonColor(directionButtons[i], Directions[i] == tunnel.facing);
                    tunnelQueue.Show(tunnel.queue); tunnelQueue.LimitAdds();
                }
            }
        }

        // Seçili yön yeşil, diğerleri sarı: VectorArt'ın çizdiği buton katmanının palet sınıfı değişir.
        private static void SetButtonColor(Button button, bool green)
        {
            var art = button.Q(className: "raster-button-art");
            if (art != null) { art.EnableInClassList("vector-green", green); art.EnableInClassList("vector-yellow", !green); }
            button.EnableInClassList("green", green); button.EnableInClassList("yellow", !green);
        }

        private void OnEditorGeometry(GeometryChangedEvent evt) => ApplyEditorLayout(layoutMode.Mode);
        private void ApplyEditorLayout(LayoutMode mode)
        {
            uiRoot.EnableInClassList("portrait", mode == LayoutMode.Portrait);
            uiRoot.EnableInClassList("square", mode == LayoutMode.Square);
            uiRoot.EnableInClassList("landscape", mode == LayoutMode.Landscape);
            float w = uiRoot.resolvedStyle.width, h = uiRoot.resolvedStyle.height;
            if (float.IsNaN(w) || float.IsNaN(h)) return;
            Rect safe = layoutMode.SafeViewport;
            uiRoot.style.paddingLeft = w * safe.xMin; uiRoot.style.paddingRight = w * (1f - safe.xMax);
            uiRoot.style.paddingTop = h * (1f - safe.yMax); uiRoot.style.paddingBottom = h * safe.yMin;
        }

        // Boş flex kapsayıcılar seçimi yok sayar, board erişilebilir kalır.
        public bool BlocksBoard(Vector2 screen)
        {
            if (documentRoot == null || documentRoot.panel == null) return false;
            if (dragging != null) return true;
            var point = RuntimePanelUtils.ScreenToPanel(documentRoot.panel, new Vector2(screen.x, Screen.height - screen.y));
            return documentRoot.panel.Pick(point) != null;
        }

        private Vector2 PanelToScreen(Vector2 point) => new Vector2(point.x * Screen.width / documentRoot.resolvedStyle.width,
            Screen.height - point.y * Screen.height / documentRoot.resolvedStyle.height);
        private void MoveGhost(Vector2 point)
        {
            Vector2 local = uiRoot.WorldToLocal(point); ghost.style.translate = new Translate(local.x - 44f, local.y - 60f);
        }
        private void CancelDrag()
        {
            var active = dragging; dragging = null;
            if (ghost != null) ghost.AddToClassList("hidden");
            active?.Cancel();
        }

        private sealed class PaletteEntry
        {
            public readonly VisualElement Element;
            private readonly LevelEditorPanel owner;
            private readonly PassengerColor color;
            private readonly Label count;
            private Vector2 origin;
            private int pointer = -1;
            private bool moved;

            public PaletteEntry(LevelEditorPanel owner, PassengerColor color)
            {
                this.owner = owner; this.color = color;
                Element = new VisualElement { name = "palette-" + color, focusable = true, tooltip = "Tap, then tap tiles to place. Or drag onto a tile." };
                Element.AddToClassList("editor-palette-item");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("editor-passenger-icon"); icon.style.unityBackgroundImageTintColor = owner.DisplayColor(color); Element.Add(icon);
                Element.Add(new Label(color.ToString()) { pickingMode = PickingMode.Ignore });
                count = new Label("0 / 0") { pickingMode = PickingMode.Ignore }; count.AddToClassList("editor-small"); Element.Add(count);
            }
            public void Bind(bool bind)
            {
                if (bind)
                {
                    Element.RegisterCallback<PointerDownEvent>(Down); Element.RegisterCallback<PointerMoveEvent>(Move);
                    Element.RegisterCallback<PointerUpEvent>(Up); Element.RegisterCallback<PointerCaptureOutEvent>(CaptureOut); Element.RegisterCallback<KeyDownEvent>(Key);
                }
                else
                {
                    Cancel(); Element.UnregisterCallback<PointerDownEvent>(Down); Element.UnregisterCallback<PointerMoveEvent>(Move);
                    Element.UnregisterCallback<PointerUpEvent>(Up); Element.UnregisterCallback<PointerCaptureOutEvent>(CaptureOut); Element.UnregisterCallback<KeyDownEvent>(Key);
                }
            }
            public void Refresh()
            {
                count.text = $"{owner.Placed(color)} / {owner.Required(color)}";
                Element.EnableInClassList("selected", owner.SelectedColor == color);
                Element.EnableInClassList("exhausted", owner.Remaining(color) <= 0); Element.EnableInClassList("overfilled", owner.Remaining(color) < 0);
            }
            private void Down(PointerDownEvent evt)
            {
                if (evt.button != 0 || owner.Draft == null) return;
                origin = evt.position; pointer = evt.pointerId; moved = false; Element.CapturePointer(pointer); evt.StopPropagation();
            }
            private void Move(PointerMoveEvent evt)
            {
                if (evt.pointerId != pointer || !Element.HasPointerCapture(pointer)) return;
                if (!moved && ((Vector2)evt.position - origin).sqrMagnitude > 64f && owner.Remaining(color) > 0)
                {
                    moved = true; owner.dragging = this; owner.ghost.style.unityBackgroundImageTintColor = owner.DisplayColor(color); owner.ghost.RemoveFromClassList("hidden");
                }
                if (moved) owner.MoveGhost(evt.position);
            }
            private void Up(PointerUpEvent evt)
            {
                if (evt.pointerId != pointer || evt.button != 0) return;
                bool drop = moved; owner.dragging = null; owner.ghost.AddToClassList("hidden");
                if (drop)
                {
                    Vector2 screen = owner.PanelToScreen(evt.position);
                    if (!owner.BlocksBoard(screen) && owner.TryScreenToCell(screen, out var cell)) owner.PlacePassenger(cell, color);
                    else owner.SetStatus("Drop the passenger on a tile of the grid.");
                }
                else owner.ToggleSelection(color);
                Cancel(); evt.StopPropagation();
            }
            public void Cancel()
            {
                int captured = pointer; pointer = -1; moved = false;
                if (captured >= 0 && Element.HasPointerCapture(captured)) Element.ReleasePointer(captured);
            }
            private void CaptureOut(PointerCaptureOutEvent evt)
            {
                if (owner.dragging == this) owner.CancelDrag();
                pointer = -1; moved = false;
            }
            private void Key(KeyDownEvent evt)
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.Space) return;
                owner.ToggleSelection(color); evt.StopPropagation();
            }
        }

        // Otobüs sırası / tünel kuyruğu: renk ekleme butonları, seçilebilir sıralı çipler ve seçili çip için
        // öne/arkaya al ve sil. Çipler ve callback'leri uzunluk değiştikçe havuzlanır. Kapsayıcı ScrollView ise
        // yatay kayar, düz VisualElement ise (otobüs sırası) çipler satıra sarılır.
        private sealed class ChipRow
        {
            private readonly LevelEditorPanel owner;
            private readonly VisualElement container;
            private readonly Action<int, int> move;
            private readonly List<Button> chips = new List<Button>();
            private readonly List<Action> handlers = new List<Action>();
            private readonly Button[] adds = new Button[Colors.Length];
            private readonly Action[] addHandlers = new Action[Colors.Length];
            private readonly Button left, right, delete;
            private readonly Action leftHandler, rightHandler, deleteHandler;
            private readonly bool limitAdds;
            private int count, selected = -1;
            private bool bound;

            public ChipRow(LevelEditorPanel owner, VisualElement root, string prefix, Action<PassengerColor> add, Action<int> remove, Action<int, int> move)
            {
                this.owner = owner; this.move = move; limitAdds = prefix == "tunnel";
                container = root.Q(prefix + "-scroll");
                container.contentContainer.style.flexDirection = FlexDirection.Row;
                var addRow = root.Q(prefix + "-add");
                for (int i = 0; i < Colors.Length; i++)
                {
                    var color = Colors[i];
                    var button = new Button { text = "+", tooltip = "Add " + color, name = prefix + "-add-" + color };
                    button.AddToClassList("editor-color-button"); button.AddToClassList("editor-add-button"); button.style.backgroundColor = owner.DisplayColor(color);
                    adds[i] = button; addRow.Add(button);
                    addHandlers[i] = () =>
                    {
                        int before = count; add(color);
                        if (count > before) Select(count - 1);
                    };
                }
                left = root.Q<Button>(prefix + "-left"); right = root.Q<Button>(prefix + "-right"); delete = root.Q<Button>(prefix + "-remove");
                leftHandler = () => Move(-1); rightHandler = () => Move(1);
                deleteHandler = () => { if (selected < 0) return; int index = selected; Select(-1); remove(index); };
                Select(-1);
            }

            public void Bind(bool bind)
            {
                if (bound == bind) return;
                bound = bind;
                for (int i = 0; i < adds.Length; i++) { if (bind) adds[i].clicked += addHandlers[i]; else adds[i].clicked -= addHandlers[i]; }
                for (int i = 0; i < chips.Count; i++) { if (bind) chips[i].clicked += handlers[i]; else chips[i].clicked -= handlers[i]; }
                if (bind) { left.clicked += leftHandler; right.clicked += rightHandler; delete.clicked += deleteHandler; }
                else { left.clicked -= leftHandler; right.clicked -= rightHandler; delete.clicked -= deleteHandler; }
            }

            public void Select(int index)
            {
                selected = index >= 0 && index < count ? index : -1;
                for (int i = 0; i < chips.Count; i++) chips[i].EnableInClassList("selected", i == selected);
                left.SetEnabled(selected > 0); right.SetEnabled(selected >= 0 && selected < count - 1); delete.SetEnabled(selected >= 0);
                if (selected >= 0 && container is ScrollView scroll) scroll.ScrollTo(chips[selected]);
            }

            private void Move(int delta)
            {
                if (selected < 0) return;
                int target = Mathf.Clamp(selected + delta, 0, count - 1);
                move(selected, delta); Select(target);
            }

            public void Show(IReadOnlyList<PassengerColor> colors)
            {
                count = colors.Count;
                while (chips.Count < colors.Count)
                {
                    int index = chips.Count;
                    var chip = new Button(); chip.AddToClassList("editor-color-button");
                    Action handler = () => Select(selected == index ? -1 : index); chips.Add(chip); handlers.Add(handler); container.Add(chip);
                    if (bound) chip.clicked += handler;
                }
                for (int i = 0; i < chips.Count; i++)
                {
                    bool used = i < colors.Count; chips[i].EnableInClassList("hidden", !used);
                    if (!used) continue;
                    chips[i].text = (i + 1).ToString(); chips[i].tooltip = $"{colors[i]} · tap to move or remove"; chips[i].style.backgroundColor = owner.DisplayColor(colors[i]);
                }
                Select(selected);
            }

            // Tünel kuyruğu yalnız otobüsü olan ve yerleştirilecek yolcusu kalan renkleri alır. Butonlar soluk görünür
            // ama tıklanabilir kalır: basınca durum satırı nedenini söyler.
            public void LimitAdds()
            {
                for (int i = 0; i < adds.Length; i++) adds[i].EnableInClassList("unavailable", limitAdds && owner.Remaining(Colors[i]) <= 0);
            }
        }
    }
}
