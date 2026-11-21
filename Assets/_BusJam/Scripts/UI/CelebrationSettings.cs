using BusJam.Gameplay;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Inspector-assigned assets for the isolated Complete-screen presentation.</summary>
    [CreateAssetMenu(menuName = "BusJam/Celebration Settings")]
    public sealed class CelebrationSettings : ScriptableObject
    {
        public ResultTimeline resultTimeline;
        public CelebrationVehicle busPrefab;
        public Passenger passengerPrefab;
        public AnimationClip drive, exhaust, idle, wave;
        public AnimationClip cheerJump, cheerLean, cheerWave;
        public AnimationClip celebrateArrive, celebrateHold, celebrateLeave;
        [Tooltip("Number of pooled riders standing on the compact bus seats."), Range(1, 2)]
        public int riderCount = 2;
        [Tooltip("Use Cheer_Wave instead of Cheer_Lean for the rear rider.")]
        public bool rearRiderWaves;
        [Tooltip("Existing shared materials, in PassengerColor order.")]
        public Material[] colors;
        public Color keyColor = new Color(1f, .94f, .86f);
        public float keyIntensity = .75f;
    }
}
