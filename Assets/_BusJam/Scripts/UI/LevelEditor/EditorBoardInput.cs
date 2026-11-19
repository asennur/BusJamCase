using BusJam.Level;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BusJam.LevelEditor
{
    /// <summary>
    /// Transparent full-screen catcher behind the editor panels. Left click cycles the tile type, right click on a
    /// tunnel opens its panel, double right click on a passenger opens its options, and dragging a passenger moves it
    /// (dropping it off the grid removes it).
    /// </summary>
    public class EditorBoardInput : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private LevelEditorPanel editor;
        [SerializeField] private EditorDragGhost ghost;
        [SerializeField] private PassengerOptionsPopup passengerPopup;
        [SerializeField] private TunnelPanel tunnelPanel;
        [SerializeField, Min(0.1f)] private float doubleClickSeconds = 0.35f;

        private float lastRightClick = -1f;
        private Vector2Int lastRightCell = new Vector2Int(-1, -1);
        private bool dragging;
        private Vector2Int dragFrom;

        private void OnEnable() => editor.TunnelSelected += OpenTunnel;
        private void OnDisable() => editor.TunnelSelected -= OpenTunnel;

        private void OpenTunnel(Vector2Int cell) => tunnelPanel.Show(cell);

        public void OnPointerClick(PointerEventData pointer)
        {
            if (pointer.dragging || !editor.TryScreenToCell(pointer.position, out var cell)) return;
            var data = editor.GetCell(cell);

            if (pointer.button == PointerEventData.InputButton.Left)
            {
                if (data.type != CellType.Passenger) editor.CycleTile(cell);
                return;
            }

            if (pointer.button != PointerEventData.InputButton.Right) return;
            if (data.type == CellType.Tunnel)
            {
                tunnelPanel.Show(cell);
                return;
            }
            if (data.type != CellType.Passenger) return;

            bool isDouble = cell == lastRightCell && Time.unscaledTime - lastRightClick <= doubleClickSeconds;
            lastRightClick = Time.unscaledTime;
            lastRightCell = cell;
            if (isDouble)
            {
                lastRightClick = -1f;
                passengerPopup.Show(cell, pointer.position);
            }
        }

        public void OnBeginDrag(PointerEventData pointer)
        {
            dragging = pointer.button == PointerEventData.InputButton.Left
                && editor.TryScreenToCell(pointer.pressPosition, out dragFrom)
                && editor.GetCell(dragFrom).type == CellType.Passenger;
            if (dragging) ghost.Begin(editor.DisplayColor(editor.GetCell(dragFrom).color), pointer.position);
        }

        public void OnDrag(PointerEventData pointer)
        {
            if (dragging) ghost.Move(pointer.position);
        }

        public void OnEndDrag(PointerEventData pointer)
        {
            if (!dragging) return;
            dragging = false;
            ghost.End();
            if (editor.TryScreenToCell(pointer.position, out var to)) editor.MovePassenger(dragFrom, to);
            else editor.RemovePassenger(dragFrom);
        }

        private void Update()
        {
            // R rotates the tunnel whose panel is open.
            if (tunnelPanel.IsOpen && Input.GetKeyDown(KeyCode.R)) editor.RotateTunnel(tunnelPanel.Cell);
        }
    }
}
