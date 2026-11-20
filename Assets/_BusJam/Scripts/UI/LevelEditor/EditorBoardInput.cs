using BusJam.Level;
using UnityEngine;

namespace BusJam.LevelEditor
{
    /// <summary>Routes board clicks only when no UI Toolkit control is under the pointer.</summary>
    public sealed class EditorBoardInput : MonoBehaviour
    {
        [SerializeField] private LevelEditorPanel editor;
        private void Update()
        {
            if (editor.Draft == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) editor.ClearSelection();
            if (editor.TunnelIsOpen && Input.GetKeyDown(KeyCode.R)) editor.RotateTunnel(editor.OpenTunnelCell);
            int button = Input.GetMouseButtonDown(0) ? 0 : Input.GetMouseButtonDown(1) ? 1 : Input.GetMouseButtonDown(2) ? 2 : -1;
            if (button >= 0) ProcessClick(Input.mousePosition, button);
        }
        public void ProcessClick(Vector2 screen, int button)
        {
            if (editor.Draft == null || editor.BlocksBoard(screen) || !editor.TryScreenToCell(screen, out var cell)) return;
            var data = editor.GetCell(cell);
            switch (button)
            {
                case 0:
                    if (editor.SelectedColor.HasValue) editor.PlacePassenger(cell, editor.SelectedColor.Value);
                    else if (data.type == CellType.Passenger) editor.Hint("This is a passenger: right-click for its options, middle-click to remove it.");
                    else editor.CycleTile(cell);
                    break;
                case 1:
                    if (data.type == CellType.Passenger) editor.ShowPassenger(cell);
                    else if (data.type == CellType.Tunnel) editor.ShowTunnel(cell);
                    break;
                case 2:
                    if (data.type == CellType.Passenger) editor.RemovePassenger(cell);
                    break;
            }
        }
    }
}
