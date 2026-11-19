using BusJam.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusJam.LevelEditor
{
    /// <summary>One colour in the passenger palette: shows how many are still to place and is dragged onto a tile.</summary>
    public class PaletteItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text count;

        private LevelEditorPanel editor;
        private EditorDragGhost ghost;
        private PassengerColor color;
        private bool dragging, usable;

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
            int remaining = editor.Remaining(color);
            usable = remaining > 0;
            // Count = passengers of this colour still to place (3 per bus, tunnels included); only usable colours can be dragged.
            count.text = remaining >= 0 ? $"x{remaining}" : $"+{-remaining}";
            count.color = remaining < 0 ? new Color(1f, 0.45f, 0.4f) : usable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            var tint = editor.DisplayColor(color);
            tint.a = usable ? 1f : 0.3f;
            icon.color = tint;
        }

        public void OnBeginDrag(PointerEventData pointer)
        {
            dragging = usable && pointer.button == PointerEventData.InputButton.Left;
            if (dragging) ghost.Begin(editor.DisplayColor(color), pointer.position);
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
