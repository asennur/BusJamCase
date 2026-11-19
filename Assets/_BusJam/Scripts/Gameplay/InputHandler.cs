using System;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Dispatches passenger input before the screen tap that starts play.</summary>
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private LayerMask passengerMask;

        public event Action<Passenger> PassengerTapped;
        public event Action ScreenTapped;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                HandleTap();
        }

        // Passenger input runs while still Ready; the screen event then starts play.
        private void HandleTap()
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, passengerMask) &&
                hit.collider.TryGetComponent(out Passenger passenger))
            {
                PassengerTapped?.Invoke(passenger);
            }

            ScreenTapped?.Invoke();
        }
    }
}
