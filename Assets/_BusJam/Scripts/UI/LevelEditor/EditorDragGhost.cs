using UnityEngine;
using UnityEngine.UI;

namespace BusJam.LevelEditor
{
    /// <summary>Passenger icon that follows the pointer while a passenger is dragged onto or across the grid.</summary>
    public class EditorDragGhost : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Canvas canvas;

        private void Awake()
        {
            icon.raycastTarget = false;
            icon.gameObject.SetActive(false);
        }

        public void Begin(Color color, Vector2 screen)
        {
            icon.color = color;
            icon.gameObject.SetActive(true);
            icon.transform.SetAsLastSibling();
            Move(screen);
        }

        public void Move(Vector2 screen)
        {
            var parent = (RectTransform)icon.rectTransform.parent;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out var local))
                icon.rectTransform.anchoredPosition = local;
        }

        public void End() => icon.gameObject.SetActive(false);
    }
}
