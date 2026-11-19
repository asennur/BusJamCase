using System.Collections.Generic;
using UnityEngine;

namespace BusJam.LevelEditor
{
    /// <summary>Scrollable left column: one draggable passenger per colour with its placed / needed count.</summary>
    public class PassengerPalette : MonoBehaviour
    {
        [SerializeField] private LevelEditorPanel editor;
        [SerializeField] private EditorDragGhost ghost;
        [Tooltip("Inactive template inside the scroll content; cloned once per colour")]
        [SerializeField] private PaletteItem template;

        private readonly List<PaletteItem> items = new List<PaletteItem>();

        private void Start()
        {
            template.gameObject.SetActive(false);
            foreach (var color in LevelEditorPanel.Colors)
            {
                var item = Instantiate(template, template.transform.parent);
                item.gameObject.SetActive(true);
                item.name = $"Palette_{color}";
                item.Setup(editor, ghost, color);
                items.Add(item);
            }
            editor.Changed += Refresh;
        }

        private void OnDestroy()
        {
            if (editor != null) editor.Changed -= Refresh;
        }

        private void Refresh()
        {
            foreach (var item in items) item.Refresh();
        }
    }
}
