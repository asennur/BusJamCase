using System;
using System.Collections.Generic;
using BusJam.Core;
using BusJam.Level;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.LevelEditor
{
    // UI Toolkit bindings for the existing draft editor API.
    public partial class LevelEditorPanel
    {
        [Header("UI Toolkit")]
        [SerializeField] private UIDocument document;
        [SerializeField] private LayoutModeController layoutMode;
        [SerializeField] private StyleSheet theme;
        private VisualElement panelRoot;
        private bool ownsPanelTheme;
        private VisualElement documentRoot, uiRoot, passengerOverlay, tunnelOverlay, ghost;
        private Label busSummary, passengerTitle, ropeLabel, tunnelTitle;
        private Toggle hiddenPassenger;
        private DropdownField cloakList;
        private Button[] buttons;
        private Action[] actions;
        private PaletteEntry[] palette;
        private ChipRow buses, tunnelQueue;
        private Vector2Int passengerCell, tunnelCell;
        private bool bound;
        private PaletteEntry dragging;

        public bool TunnelIsOpen => tunnelOverlay != null && !tunnelOverlay.ClassListContains("hidden");
        public Vector2Int OpenTunnelCell => tunnelCell;

        private void OnEnable()
        {
            if (document == null || layoutMode == null || database == null) return;
            CacheToolkit();
            if (uiRoot == null || bound) return;
            bound = true;
            documentRoot.RegisterCallback<AttachToPanelEvent>(OnPanelAttached);
            ApplyPanelTheme(documentRoot.panel);
            for (int i = 0; i < buttons.Length; i++) buttons[i].clicked += actions[i];
            levelList.RegisterValueChangedCallback(OnLevelSelected);
            seconds.RegisterCallback<FocusOutEvent>(OnTimeChanged);
            hiddenPassenger.RegisterValueChangedCallback(OnHiddenChanged);
            cloakList.RegisterValueChangedCallback(OnCloakChanged);
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
            uiRoot.UnregisterCallback<GeometryChangedEvent>(OnEditorGeometry);
            layoutMode.Changed -= ApplyEditorLayout;
            Changed -= RefreshToolkit; TunnelSelected -= ShowTunnel;
            foreach (var entry in palette) entry.Bind(false);
            buses.Bind(false); tunnelQueue.Bind(false);
        }

        private void CacheToolkit()
        {
            if (documentRoot == document.rootVisualElement) return;
            documentRoot = document.rootVisualElement;
            documentRoot.pickingMode = PickingMode.Ignore;
            uiRoot = documentRoot.Q("editor-root");
            if (uiRoot == null) return;
            BusJam.UI.VectorArt.Apply(uiRoot);
            levelList = uiRoot.Q<DropdownField>("level-list");
            width = uiRoot.Q<TextField>("width"); height = uiRoot.Q<TextField>("height"); seconds = uiRoot.Q<TextField>("seconds");
            status = uiRoot.Q<Label>("status"); solverResult = uiRoot.Q<Label>("solver-result"); busSummary = uiRoot.Q<Label>("bus-summary");
            passengerOverlay = uiRoot.Q("passenger-overlay"); tunnelOverlay = uiRoot.Q("tunnel-overlay");
            passengerTitle = uiRoot.Q<Label>("passenger-title"); ropeLabel = uiRoot.Q<Label>("ropes"); tunnelTitle = uiRoot.Q<Label>("tunnel-title");
            hiddenPassenger = uiRoot.Q<Toggle>("hidden-passenger"); cloakList = uiRoot.Q<DropdownField>("cloak");
            cloakList.choices = new List<string> { "No cloak", "Cloak · starts open", "Cloak · starts closed" }; cloakList.index = 0;
            ghost = uiRoot.Q("passenger-ghost");
            string[] names = { "load", "new", "save", "test", "apply", "auto-grid", "solve", "auto-fill", "passenger-close", "ropes-minus", "ropes-plus", "delete-passenger", "tunnel-close", "rotate-tunnel" };
            actions = new Action[] { LoadSelected, NewLevel, SaveDraft, TestPlay, ApplySize, AutoGrid, Solve, AutoFill, HidePassenger, RemoveRope, AddRope, DeletePassenger, HideTunnel, RotateOpenTunnel };
            buttons = new Button[names.Length];
            for (int i = 0; i < names.Length; i++) buttons[i] = uiRoot.Q<Button>(names[i]);
            buses = new ChipRow(this, uiRoot.Q("bus-add"), uiRoot.Q<ScrollView>("bus-scroll"), AddBus, RemoveBusAt);
            tunnelQueue = new ChipRow(this, uiRoot.Q("tunnel-add"), uiRoot.Q<ScrollView>("tunnel-scroll"), AddTunnelPassenger, RemoveTunnelPassenger);
            var container = uiRoot.Q<ScrollView>("palette");
            palette = new PaletteEntry[Colors.Length];
            for (int i = 0; i < Colors.Length; i++) { palette[i] = new PaletteEntry(this, Colors[i]); container.Add(palette[i].Element); }
        }

        private void SaveDraft() => Save();
        // Native dropdowns are panel siblings, so they also need the shared sheet.
        private void OnPanelAttached(AttachToPanelEvent evt) => ApplyPanelTheme(evt.destinationPanel);
        private void ApplyPanelTheme(IPanel panel)
        {
            if (panel == null || theme == null || ownsPanelTheme) return;
            panelRoot = panel.visualTree;
            if (panelRoot.styleSheets.Contains(theme)) return;
            panelRoot.styleSheets.Add(theme); ownsPanelTheme = true;
        }
        private void OnLevelSelected(ChangeEvent<string> evt) => LoadSelected();
        private void OnTimeChanged(FocusOutEvent evt) { if (draft != null) ApplyTime(); }
        private void OnHiddenChanged(ChangeEvent<bool> evt) => SetHidden(passengerCell, evt.newValue);
        private void OnCloakChanged(ChangeEvent<string> evt) => SetCloak(passengerCell, (Cloak)cloakList.index);
        private void RemoveRope() => SetRopes(passengerCell, GetCell(passengerCell).ropes - 1);
        private void AddRope() => SetRopes(passengerCell, GetCell(passengerCell).ropes + 1);
        private void DeletePassenger() { RemovePassenger(passengerCell); HidePassenger(); }
        private void HidePassenger() => passengerOverlay.AddToClassList("hidden");
        private void HideTunnel() => tunnelOverlay.AddToClassList("hidden");
        private void RotateOpenTunnel() => RotateTunnel(tunnelCell);
        private void AddTunnelPassenger(PassengerColor color) => AddToTunnel(tunnelCell, color);
        private void RemoveTunnelPassenger(int index) => RemoveFromTunnel(tunnelCell, index);

        public void ShowPassenger(Vector2Int cell)
        {
            passengerCell = cell; HideTunnel(); passengerOverlay.RemoveFromClassList("hidden"); RefreshToolkit();
        }

        public void ShowTunnel(Vector2Int cell)
        {
            tunnelCell = cell; HidePassenger(); tunnelOverlay.RemoveFromClassList("hidden"); RefreshToolkit();
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
                    passengerTitle.text = $"{data.color} passenger ({passengerCell.x}, {passengerCell.y})";
                    hiddenPassenger.SetValueWithoutNotify(data.hidden); hiddenPassenger.label = data.hidden ? "Hidden (?): ON" : "Hidden (?): OFF";
                    ropeLabel.text = $"Ropes: {data.ropes}"; cloakList.SetValueWithoutNotify(cloakList.choices[(int)data.cloak]);
                }
            }
            if (TunnelIsOpen)
            {
                var tunnel = TunnelAt(tunnelCell);
                if (tunnel == null) HideTunnel();
                else
                {
                    tunnelTitle.text = $"Tunnel ({tunnelCell.x}, {tunnelCell.y}) · faces {tunnel.facing} · {tunnel.queue.Count} inside";
                    tunnelQueue.Show(tunnel.queue); tunnelQueue.LimitAdds();
                }
            }
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

        // Empty flex containers ignore picking, leaving the board reachable.
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
                Element = new VisualElement { name = "palette-" + color, focusable = true, tooltip = "Click to place, or drag onto a tile" };
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

        // Pool chip buttons and their callbacks as bus/queue lengths change.
        private sealed class ChipRow
        {
            private readonly LevelEditorPanel owner;
            private readonly ScrollView container;
            private readonly Action<int> remove;
            private readonly List<Button> chips = new List<Button>();
            private readonly List<Action> handlers = new List<Action>();
            private readonly Button[] adds = new Button[Colors.Length];
            private readonly Action[] addHandlers = new Action[Colors.Length];
            private bool bound;
            public ChipRow(LevelEditorPanel owner, VisualElement addRow, ScrollView container, Action<PassengerColor> add, Action<int> remove)
            {
                this.owner = owner; this.container = container; this.remove = remove;
                container.contentContainer.style.flexDirection = FlexDirection.Row;
                for (int i = 0; i < Colors.Length; i++)
                {
                    var color = Colors[i];
                    var button = new Button { text = "+", tooltip = "Add " + color, name = "add-" + color };
                    button.AddToClassList("editor-color-button"); button.style.backgroundColor = owner.DisplayColor(color);
                    adds[i] = button; addHandlers[i] = () => add(color); addRow.Add(button);
                }
            }
            public void Bind(bool bind)
            {
                if (bound == bind) return;
                bound = bind;
                for (int i = 0; i < adds.Length; i++) { if (bind) adds[i].clicked += addHandlers[i]; else adds[i].clicked -= addHandlers[i]; }
                for (int i = 0; i < chips.Count; i++) { if (bind) chips[i].clicked += handlers[i]; else chips[i].clicked -= handlers[i]; }
            }
            public void Show(IReadOnlyList<PassengerColor> colors)
            {
                while (chips.Count < colors.Count)
                {
                    int index = chips.Count;
                    var chip = new Button(); chip.AddToClassList("editor-color-button");
                    Action handler = () => remove(index); chips.Add(chip); handlers.Add(handler); container.Add(chip);
                    if (bound) chip.clicked += handler;
                }
                for (int i = 0; i < chips.Count; i++)
                {
                    bool used = i < colors.Count; chips[i].EnableInClassList("hidden", !used);
                    if (!used) continue;
                    chips[i].text = (i + 1).ToString(); chips[i].tooltip = "Remove " + colors[i]; chips[i].style.backgroundColor = owner.DisplayColor(colors[i]);
                }
            }
            public void LimitAdds()
            {
                for (int i = 0; i < adds.Length; i++) adds[i].SetEnabled(owner.Remaining(Colors[i]) > 0);
            }
        }
    }
}
