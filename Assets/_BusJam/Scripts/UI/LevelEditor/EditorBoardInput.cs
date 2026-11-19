using BusJam.Level;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BusJam.LevelEditor
{
    /// <summary>
    /// Transparent full-screen catcher behind the editor panels.
    /// Colour selected in the palette: left click places that passenger on a tile.
    /// Nothing selected: left click cycles the tile (Tile > Hole > Obstacle > Tunnel).
    /// Always: right click opens passenger options or the tunnel panel, middle click removes a passenger.
    /// </summary>
    public class EditorBoardInput : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private LevelEditorPanel editor;
        [SerializeField] private PassengerOptionsPopup passengerPopup;
        [SerializeField] private TunnelPanel tunnelPanel;

        private void OnEnable() => editor.TunnelSelected += OpenTunnel;
        private void OnDisable() => editor.TunnelSelected -= OpenTunnel;

        private void OpenTunnel(Vector2Int cell) => tunnelPanel.Show(cell);

        public void OnPointerClick(PointerEventData pointer)
        {
            if (!editor.TryScreenToCell(pointer.position, out var cell)) return;
            var data = editor.GetCell(cell);

            switch (pointer.button)
            {
                case PointerEventData.InputButton.Left:
                    if (editor.SelectedColor.HasValue) editor.PlacePassenger(cell, editor.SelectedColor.Value);
                    else if (data.type == CellType.Passenger) editor.Hint("This is a passenger: right-click for its options, middle-click to remove it.");
                    else editor.CycleTile(cell);
                    break;

                case PointerEventData.InputButton.Middle:
                    if (data.type == CellType.Passenger) editor.RemovePassenger(cell);
                    break;

                case PointerEventData.InputButton.Right:
                    if (data.type == CellType.Passenger) passengerPopup.Show(cell, pointer.position);
                    else if (data.type == CellType.Tunnel) tunnelPanel.Show(cell);
                    break;
            }
        }

        private void Update()
        {
            // R rotates the tunnel whose panel is open; Escape stops placing.
            if (tunnelPanel.IsOpen && Input.GetKeyDown(KeyCode.R)) editor.RotateTunnel(tunnelPanel.Cell);
            if (Input.GetKeyDown(KeyCode.Escape)) editor.ClearSelection();
        }
    }
}
