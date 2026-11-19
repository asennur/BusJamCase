using System.Collections.Generic;
using BusJam.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.LevelEditor
{
    /// <summary>Opens for the selected tunnel: rotate it and build the queue of passengers that come out of it.</summary>
    public class TunnelPanel : MonoBehaviour
    {
        [SerializeField] private LevelEditorPanel editor;
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private ColorChipList queue;
        [SerializeField] private Button rotateButton, closeButton;

        public bool IsOpen => root.activeSelf;
        public Vector2Int Cell { get; private set; }

        private void Awake()
        {
            root.SetActive(false);
            rotateButton.onClick.AddListener(() => editor.RotateTunnel(Cell));
            closeButton.onClick.AddListener(Hide);
        }

        private void Start()
        {
            queue.BuildAddButtons(editor, color => editor.AddToTunnel(Cell, color));
            queue.SetRemoveHandler(index => editor.RemoveFromTunnel(Cell, index));
            editor.Changed += Refresh;
        }

        private void OnDestroy()
        {
            if (editor != null) editor.Changed -= Refresh;
        }

        public void Show(Vector2Int cell)
        {
            Cell = cell;
            root.SetActive(true);
            Refresh();
        }

        public void Hide() => root.SetActive(false);

        private void Refresh()
        {
            if (!IsOpen) return;
            var tunnel = editor.TunnelAt(Cell);
            if (tunnel == null) { Hide(); return; }
            title.text = $"Tunnel ({Cell.x}, {Cell.y}) · faces {tunnel.facing} · {tunnel.queue.Count} inside";
            queue.Show(editor, tunnel.queue);
        }
    }
}
