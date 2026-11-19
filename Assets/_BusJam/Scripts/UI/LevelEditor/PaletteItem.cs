using BusJam.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BusJam.LevelEditor
{
    /// <summary>
    /// One colour in the passenger palette. Click to select it (outlined), then left-click tiles to place passengers;
    /// click it again to stop placing. Shows how many of this colour are still to place (3 per bus, tunnels included).
    /// </summary>
    public class PaletteItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text count;

        private LevelEditorPanel editor;
        private PassengerColor color;
        private bool usable;
        private Outline outline;

        public void Setup(LevelEditorPanel owner, PassengerColor passengerColor)
        {
            editor = owner;
            color = passengerColor;
            outline = icon.GetComponent<Outline>();
            if (outline == null) outline = icon.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
            outline.effectDistance = new Vector2(5f, -5f);
            Refresh();
        }

        public void Refresh()
        {
            if (editor == null || editor.Draft == null) return;
            int remaining = editor.Remaining(color);
            usable = remaining > 0;
            count.text = remaining >= 0 ? $"x{remaining}" : $"+{-remaining}";
            count.color = remaining < 0 ? new Color(1f, 0.45f, 0.4f) : usable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            var tint = editor.DisplayColor(color);
            tint.a = usable ? 1f : 0.3f;
            icon.color = tint;

            bool selected = editor.SelectedColor == color;
            outline.enabled = selected;
            transform.localScale = selected ? Vector3.one * 1.12f : Vector3.one;
        }

        public void OnPointerClick(PointerEventData pointer)
        {
            if (pointer.button == PointerEventData.InputButton.Left) editor.ToggleSelection(color);
        }
    }
}
