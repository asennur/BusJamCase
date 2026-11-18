using System;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Dokunma girişini yolcu tap'ı veya ekran tap'ına dönüştürür.</summary>
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private Camera cam;

        public event Action<Passenger> PassengerTapped;
        public event Action ScreenTapped;

        private void Start()
        {
            if (cam == null)
                cam = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                HandleTap();
        }

        private void HandleTap()
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMask.GetMask("Passenger")))
            {
                var passenger = hit.collider.GetComponent<Passenger>();
                if (passenger != null)
                {
                    PassengerTapped?.Invoke(passenger);
                    return;
                }
            }

            ScreenTapped?.Invoke();
        }
    }
}
