using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Gridde bir yolcunun görünümü ve verisi. Karar vermez; ne yapacağını GameplayController söyler.</summary>
    public class Passenger : MonoBehaviour
    {
        [SerializeField] private Renderer body;
        [SerializeField] private GameObject outline;

        public PassengerColor Color { get; private set; }
        public Vector2Int Cell { get; private set; }
        public bool IsHidden { get; private set; }

        public void Setup(PassengerColor color, Vector2Int cell, bool hidden, Material material)
        {
            Color = color;
            Cell = cell;
            IsHidden = hidden;
            body.sharedMaterial = material;
            outline.SetActive(false);
        }

        /// <summary>Erişilebilir yolcu outline'lı görünür.</summary>
        public void SetReachable(bool reachable)
        {
            if (outline.activeSelf != reachable)
                outline.SetActive(reachable);
        }
    }
}
