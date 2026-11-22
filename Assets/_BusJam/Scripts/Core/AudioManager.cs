using System;
using BusJam.Gameplay;
using UnityEngine;

namespace BusJam.Core
{
    /// <summary>Oyunun çaldığı ses efektleri.</summary>
    public enum Sfx
    {
        Start,
        Button,
        PassengerTap,
        BusDepart,
        Win,
        Lose
    }

    /// <summary>
    /// Kısa ses efektlerini tek AudioSource ile çalar. Oyun olayları (başlama, kazanma, kaybetme, otobüs kalkışı)
    /// GameManager ve BusStation dinlenerek duyulur; yolcu dokunuşu için oynanış kodu AudioManager.Play çağırır;
    /// UI Toolkit controller'ları buton seslerini Play ile ister.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public Sfx sound;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [SerializeField] private AudioSource source;
        [SerializeField] private Entry[] sounds;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BusStation busStation;

        private static AudioManager instance;
        private Entry[] bySound;

        private const string MutedKey = "SoundMuted";

        /// <summary>Ayarlar panelinden ses açık/kapalı; oyun yeniden başlasa da korunsun diye kaydedilir.</summary>
        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
                PlayerPrefs.Save();
                AudioListener.volume = value ? 0f : 1f;
            }
        }

        /// <summary>Sahnede AudioManager varsa ve bu ses için klibi varsa sesi çalar.</summary>
        public static void Play(Sfx sound)
        {
            if (instance != null) instance.PlayClip(sound);
        }

        private void Awake()
        {
            instance = this;
            AudioListener.volume = Muted ? 0f : 1f;
            // Inspector listesi bir kez enum değerine göre indekslenir, böylece Play doğrudan dizi okuması olur.
            bySound = new Entry[Enum.GetValues(typeof(Sfx)).Length];
            foreach (var entry in sounds) bySound[(int)entry.sound] = entry;
        }

        private void OnEnable()
        {
            if (gameManager != null) gameManager.StateChanged += OnStateChanged;
            if (busStation != null) busStation.BusDeparting += OnBusDeparting;
        }

        private void OnDisable()
        {
            if (gameManager != null) gameManager.StateChanged -= OnStateChanged;
            if (busStation != null) busStation.BusDeparting -= OnBusDeparting;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing) Play(Sfx.Start);
            else if (state == GameState.Complete) Play(Sfx.Win);
            else if (state == GameState.Fail) Play(Sfx.Lose);
        }

        private void OnBusDeparting(Bus bus) => Play(Sfx.BusDepart);

        private void PlayClip(Sfx sound)
        {
            Entry entry = bySound[(int)sound];
            if (entry.clip == null || source == null) return;
            source.PlayOneShot(entry.clip, entry.volume <= 0f ? 1f : entry.volume);
        }
    }
}
