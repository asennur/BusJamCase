using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BusJam.Gameplay
{
    /// <summary>Yolcu girdisini, oyunu başlatan ekran dokunuşundan önce dağıtır.</summary>
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private LayerMask passengerMask;
        [Tooltip("Taps on pickable UI of this document (buttons, banner, booster bar, modals) never reach the world.")]
        [SerializeField] private UIDocument uiDocument;

        public event Action<Passenger> PassengerTapped;
        public event Action ScreenTapped;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                HandleTap();
        }

        // Yolcu girdisi hâlâ Ready iken çalışır; sonra ekran olayı oyunu başlatır.
        private void HandleTap()
        {
            if (IsOverUI(Input.mousePosition)) return;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, passengerMask) &&
                hit.collider.TryGetComponent(out Passenger passenger))
            {
                PassengerTapped?.Invoke(passenger);
            }

            ScreenTapped?.Invoke();
        }
    
        private bool IsOverUI(Vector2 screen)
        {
            IPanel panel = uiDocument != null && uiDocument.rootVisualElement != null ? uiDocument.rootVisualElement.panel : null;
            if (panel == null) return false;
            Vector2 point = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            return panel.Pick(point) != null;
        }
    }
}
