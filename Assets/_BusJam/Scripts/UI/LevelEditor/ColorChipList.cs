using System;
using System.Collections.Generic;
using BusJam.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.LevelEditor
{
    /// <summary>
    /// Reusable row of coloured chips (bus order, tunnel queue) plus one "add" button per colour.
    /// Chips are pooled; clicking a chip removes it.
    /// </summary>
    public class ColorChipList : MonoBehaviour
    {
        [Tooltip("Inactive chip template (Button + Image + optional TMP label) inside the chip container")]
        [SerializeField] private Button chipTemplate;
        [Tooltip("Inactive add-button template inside the add-button container")]
        [SerializeField] private Button addTemplate;

        private readonly List<Button> chips = new List<Button>();
        private Action<int> onRemove;
        private bool built;

        public void BuildAddButtons(LevelEditorPanel editor, Action<PassengerColor> onAdd)
        {
            if (built) return;
            built = true;
            chipTemplate.gameObject.SetActive(false);
            addTemplate.gameObject.SetActive(false);
            foreach (var color in LevelEditorPanel.Colors)
            {
                var button = Instantiate(addTemplate, addTemplate.transform.parent);
                button.gameObject.SetActive(true);
                button.name = $"Add_{color}";
                button.image.color = editor.DisplayColor(color);
                var picked = color;
                button.onClick.AddListener(() => onAdd(picked));
            }
        }

        public void SetRemoveHandler(Action<int> handler) => onRemove = handler;

        public void Show(LevelEditorPanel editor, IReadOnlyList<PassengerColor> colors)
        {
            while (chips.Count < colors.Count)
            {
                var chip = Instantiate(chipTemplate, chipTemplate.transform.parent);
                int index = chips.Count;
                chip.onClick.AddListener(() => onRemove?.Invoke(index));
                chips.Add(chip);
            }
            for (int i = 0; i < chips.Count; i++)
            {
                bool used = i < colors.Count;
                chips[i].gameObject.SetActive(used);
                if (!used) continue;
                chips[i].image.color = editor.DisplayColor(colors[i]);
                var label = chips[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = (i + 1).ToString();
            }
        }
    }
}
