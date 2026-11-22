using BusJam.Core;
using BusJam.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.LevelEditor
{
    /// <summary>Editör UI'ındaki boş board alanını (<c>.editor-board-space</c>) kameraya verir; oyun kadrajı o bölgeye
    /// telefon ekranı gibi çizilir. Yalnız yerleşim değişince çalışır.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class EditorBoardViewport : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private CameraFitter fitter;
        [SerializeField] private string boardSpaceClass = "editor-board-space";
        private VisualElement root, space;

        private void OnEnable()
        {
            root = document != null ? document.rootVisualElement : null;
            if (root == null || fitter == null) return;
            fitter.SetMinimumGrid(GameRules.MaxWidth, GameRules.MaxHeight); // her level en büyük grid kadrajında görünür
            root.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            Apply();
        }

        private void OnDisable()
        {
            if (root != null) root.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            if (space != null) space.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
            if (fitter != null) { fitter.SetScreenArea(new Rect(0, 0, 1, 1)); fitter.SetMinimumGrid(0, 0); }
            root = space = null;
        }

        private void OnGeometry(GeometryChangedEvent e) => Apply();

        // Panel koordinatı (sol üst kökenli) → normalize viewport (sol alt kökenli). Board alanı UXML yüklenince bulunur.
        private void Apply()
        {
            if (space == null || space.panel == null)
            {
                if (space != null) space.UnregisterCallback<GeometryChangedEvent>(OnGeometry);
                space = root.Q(className: boardSpaceClass);
                if (space != null) space.RegisterCallback<GeometryChangedEvent>(OnGeometry);
            }
            Rect full = root.worldBound;
            if (space == null || !(full.width > 0 && full.height > 0)) return;
            Rect area = space.worldBound;
            fitter.SetScreenArea(new Rect((area.xMin - full.xMin) / full.width, 1f - (area.yMax - full.yMin) / full.height,
                area.width / full.width, area.height / full.height));
        }
    }
}
