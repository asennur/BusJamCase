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
        [Tooltip("Optional: if set, the cloak is chosen from this dropdown (No cloak / Starts open / Starts closed) instead of the cycle button")]
        [SerializeField] private TMP_Dropdown cloakDropdown;

        private static readonly Color OnColor = new Color(0.35f, 0.85f, 0.3f), OffColor = new Color(0.55f, 0.55f, 0.6f);

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
            if (cloakDropdown != null)
            {
                cloakDropdown.ClearOptions();
                cloakDropdown.AddOptions(new System.Collections.Generic.List<string> { "No cloak", "Cloak · starts open", "Cloak · starts closed" });
                cloakDropdown.onValueChanged.AddListener(value => editor.SetCloak(cell, (Cloak)value));
                cloakButton.gameObject.SetActive(false);
            }
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
            // Make the toggle state obvious: label says ON/OFF and the box turns green when on.
            var hiddenLabel = hidden.GetComponentInChildren<TMP_Text>(true);
            if (hiddenLabel != null) hiddenLabel.text = data.hidden ? "Hidden (?): ON" : "Hidden (?): OFF";
            if (hidden.targetGraphic != null) hidden.targetGraphic.color = data.hidden ? OnColor : OffColor;
            if (hidden.graphic != null) hidden.graphic.enabled = data.hidden;
            ropes.text = $"Ropes: {data.ropes}";
            cloak.text = data.cloak switch
            {
                Cloak.StartsOpen => "Cloak: ON · starts open",
                Cloak.StartsClosed => "Cloak: ON · starts closed",
                _ => "Cloak: OFF"
            };
            if (cloakDropdown != null) cloakDropdown.SetValueWithoutNotify((int)data.cloak);
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
