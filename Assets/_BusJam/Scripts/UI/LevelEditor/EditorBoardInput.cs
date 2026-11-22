using BusJam.Level;
using UnityEngine;

namespace BusJam.LevelEditor
{
    /// <summary>Board tıklamalarını yalnız imlecin altında UI Toolkit kontrolü yokken iletir.</summary>
    public sealed class EditorBoardInput : MonoBehaviour
    {
        [SerializeField] private LevelEditorPanel editor;
        private void Update()
        {
            if (editor.Draft == null) return;
            if (Input.GetKeyDown(KeyCode.Escape) && !editor.ClosePopups()) editor.ClearSelection();
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
                    // Dokunmatikte sağ/orta tık yok: yolcu ve tünel dokununca düzenleme paneli açılır.
                    else if (data.type == CellType.Passenger) editor.ShowPassenger(cell);
                    else if (data.type == CellType.Tunnel) editor.ShowTunnel(cell);
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
