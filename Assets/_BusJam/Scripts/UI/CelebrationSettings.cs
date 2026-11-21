using BusJam.Gameplay;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Inspector-assigned assets for the isolated Complete-screen presentation.</summary>
    [CreateAssetMenu(menuName = "BusJam/Celebration Settings")]
    public sealed class CelebrationSettings : ScriptableObject
    {
        public ResultTimeline resultTimeline;
        public Bus busPrefab;
        public Passenger passengerPrefab;
        public AnimationClip drive, exhaust, idle, wave;
        [Tooltip("Existing shared materials, in PassengerColor order.")]
        public Material[] colors;
        [Range(-15, 35)] public float cameraPitch = -7;
        [Range(20, 60)] public float fieldOfView = 35;
        public Color keyColor = new Color(1f, .94f, .86f);
        public float keyIntensity = .75f;
    }
}
