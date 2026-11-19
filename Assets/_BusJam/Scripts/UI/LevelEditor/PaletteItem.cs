using BusJam.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusJam.LevelEditor
{
    /// <summary>One colour in the passenger palette: shows placed / needed (3 per bus) and is dragged onto a tile.</summary>
    public class PaletteItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text count;

        private LevelEditorPanel editor;
        private EditorDragGhost ghost;
        private PassengerColor color;
        private bool dragging;

        public void Setup(LevelEditorPanel owner, EditorDragGhost dragGhost, PassengerColor passengerColor)
        {
            editor = owner;
            ghost = dragGhost;
            color = passengerColor;
            icon.color = editor.DisplayColor(color);
            Refresh();
        }

        public void Refresh()
        {
            if (editor == null || editor.Draft == null) return;
            int placed = editor.Placed(color), needed = editor.Required(color);
            count.text = $"{placed}/{needed}";
            count.color = placed == needed ? new Color(0.55f, 1f, 0.45f) : placed > needed ? new Color(1f, 0.45f, 0.4f) : Color.white;
        }

        public void OnBeginDrag(PointerEventData pointer)
        {
            dragging = pointer.button == PointerEventData.InputButton.Left;
            if (dragging) ghost.Begin(icon.color, pointer.position);
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
            if (editor.TryScreenToCell(pointer.position, out var cell)) editor.PlacePassenger(cell, color);
        }
    }
}
