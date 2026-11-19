using TMPro;
using UnityEngine;

namespace BusJam.LevelEditor
{
    /// <summary>Bus order strip over the road: add a bus per colour, click a bus chip to remove it.</summary>
    public class BusOrderBar : MonoBehaviour
    {
        [SerializeField] private LevelEditorPanel editor;
        [SerializeField] private ColorChipList chips;
        [SerializeField] private TMP_Text summary;

        private void Start()
        {
            chips.BuildAddButtons(editor, editor.AddBus);
            chips.SetRemoveHandler(editor.RemoveBusAt);
            editor.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (editor != null) editor.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (editor.Draft == null) return;
            chips.Show(editor, editor.BusOrder);
            if (summary != null)
                summary.text = editor.BusOrder.Count == 0 ? "Buses: add at least one" : $"Buses: {editor.BusOrder.Count}  ·  passengers needed: {editor.BusOrder.Count * 3}";
        }
    }
}
