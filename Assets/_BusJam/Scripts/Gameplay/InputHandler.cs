using System;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Dokunma girişini yolcu tap'ı veya ekran tap'ına dönüştürür.</summary>
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

        // Önce PassengerTapped, sonra ScreenTapped: Start ekranında bir yolcuya denk gelen başlatma
        // dokunuşu önce oyunu başlatırsa (ScreenTapped önce olsaydı) aynı dokunuş yolcuyu da koşturur.
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
