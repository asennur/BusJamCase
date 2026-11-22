using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Küçük kutlama otobüsünde Inspector'a bağlı model ve iki ayakta durma noktası.</summary>
    public sealed class CelebrationVehicle : MonoBehaviour
    {
        public Animator model;
        public Renderer body;
        public Transform frontSeat, rearSeat;
    }
}
