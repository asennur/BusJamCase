using BusJam.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam.LevelEditor
{
    /// <summary>Double right click on a passenger: hidden, rope count, cloak, remove.</summary>
    public class PassengerOptionsPopup : MonoBehaviour
    {
        [SerializeField] private LevelEditorPanel editor;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text title;
        [SerializeField] private Toggle hidden;
        [SerializeField] private TMP_Text ropes, cloak;
        [SerializeField] private Button ropesMinus, ropesPlus, cloakButton, removeButton, closeButton;

        private Vector2Int cell;

        private void Awake()
        {
            root.gameObject.SetActive(false);
            hidden.onValueChanged.AddListener(value => editor.SetHidden(cell, value));
            ropesMinus.onClick.AddListener(() => editor.SetRopes(cell, editor.GetCell(cell).ropes - 1));
            ropesPlus.onClick.AddListener(() => editor.SetRopes(cell, editor.GetCell(cell).ropes + 1));
            cloakButton.onClick.AddListener(NextCloak);
            removeButton.onClick.AddListener(() => { editor.RemovePassenger(cell); Hide(); });
            closeButton.onClick.AddListener(Hide);
        }

        private void Start() => editor.Changed += Refresh;

        private void OnDestroy()
        {
            if (editor != null) editor.Changed -= Refresh;
        }

        public void Show(Vector2Int target, Vector2 screen)
        {
            cell = target;
            root.gameObject.SetActive(true);
            var parent = (RectTransform)root.parent;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out var local))
                root.anchoredPosition = ClampInside(parent, local);
            Refresh();
        }

        public void Hide() => root.gameObject.SetActive(false);

        private void NextCloak()
        {
            var next = editor.GetCell(cell).cloak switch
            {
                Cloak.None => Cloak.StartsOpen,
                Cloak.StartsOpen => Cloak.StartsClosed,
                _ => Cloak.None
            };
            editor.SetCloak(cell, next);
        }

        private void Refresh()
        {
            if (!root.gameObject.activeSelf) return;
            var data = editor.GetCell(cell);
            if (data.type != CellType.Passenger) { Hide(); return; }
            title.text = $"{data.color} passenger ({cell.x}, {cell.y})";
            hidden.SetIsOnWithoutNotify(data.hidden);
            ropes.text = $"Ropes: {data.ropes}";
            cloak.text = data.cloak switch
            {
                Cloak.StartsOpen => "Cloak: starts open",
                Cloak.StartsClosed => "Cloak: starts closed",
                _ => "Cloak: none"
            };
        }

        private Vector2 ClampInside(RectTransform parent, Vector2 local)
        {
            Rect area = parent.rect;
            Vector2 half = root.rect.size * 0.5f;
            return new Vector2(Mathf.Clamp(local.x, area.xMin + half.x, area.xMax - half.x),
                Mathf.Clamp(local.y, area.yMin + half.y, area.yMax - half.y));
        }
    }
}
