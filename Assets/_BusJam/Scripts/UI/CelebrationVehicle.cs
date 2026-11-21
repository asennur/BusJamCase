using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Inspector-bound model and two standing anchors on the compact celebration bus.</summary>
    public sealed class CelebrationVehicle : MonoBehaviour
    {
        public Animator model;
        public Renderer body;
        public Transform frontSeat, rearSeat;
    }
}
