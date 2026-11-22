using BusJam.Gameplay;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Complete ekranındaki yalıtılmış kutlama sahnesinin Inspector asset'leri.</summary>
    [CreateAssetMenu(menuName = "BusJam/Celebration Settings")]
    public sealed class CelebrationSettings : ScriptableObject
    {
        public ResultTimeline resultTimeline;
        public CelebrationVehicle busPrefab;
        public Passenger passengerPrefab;
        public AnimationClip drive, exhaust, idle, wave;
        [Tooltip("Cheer_Jump: ön yolcu. Cheer_Wave: arka yolcu. Cheer_Lean: otobüs hareket ederken çömelip tutunma.")]
        public AnimationClip cheerJump, cheerLean, cheerWave;
        public AnimationClip celebrateArrive, celebrateHold, celebrateLeave;
        [Tooltip("Number of pooled riders standing on the bus deck."), Range(1, 2)]
        public int riderCount = 2;

        [Header("Riders")]
        [Tooltip("Yolcu boyu, oyundaki ayakta yolcuya göre (referansta yolcu ≈ otobüs gövdesi yüksekliği).")]
        public float riderScale = 1.3f;
        [Tooltip("Ön/arka yolcunun koltuk noktasına göre yeri, otobüs modeli uzayında (y: güverte, z: kameraya doğru).")]
        public Vector3 frontRiderOffset = new Vector3(-.52f, -.21f, -.45f), rearRiderOffset = new Vector3(-.35f, -.21f, -.45f);
        [Tooltip("Ön/arka yolcunun Y dönüşü (derece, otobüs modeli uzayında).")]
        public float frontRiderYaw = 200, rearRiderYaw = 160;
        [Tooltip("Arka yolcunun kutlama klibindeki faz farkı (sn); iki yolcu aynı anda zıplamasın.")]
        public float rearCheerPhase = .3f;
        [Tooltip("Durmadan bu kadar sn önce kutlama başlar, kalkıştan bu kadar sn sonra tutunmaya döner.")]
        public float cheerLead = .12f, cheerTail = .15f;
        [Tooltip("Tutunma ile kutlama arasındaki geçiş süresi (sn)."), Min(.01f)]
        public float braceBlend = .12f;

        [Tooltip("Existing shared materials, in PassengerColor order.")]
        public Material[] colors;
        public Color keyColor = new Color(1f, .94f, .86f);
        public float keyIntensity = .75f;
    }
}
