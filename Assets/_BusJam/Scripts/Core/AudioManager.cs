using System;
using BusJam.Gameplay;
using UnityEngine;

namespace BusJam.Core
{
    /// <summary>The sound effects the game plays.</summary>
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
    /// Plays short sound effects through one AudioSource. Game events (start, win, lose, bus leaving) are heard by
    /// listening to GameManager and BusStation; gameplay code calls AudioManager.Play for the passenger tap;
    /// UI Toolkit controllers request button sounds through Play.
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

        /// <summary>Sound on/off from the settings panel; saved so it survives a restart of the game.</summary>
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

        /// <summary>Plays a sound if an AudioManager exists in the scene and has a clip for it.</summary>
        public static void Play(Sfx sound)
        {
            if (instance != null) instance.PlayClip(sound);
        }

        private void Awake()
        {
            instance = this;
            AudioListener.volume = Muted ? 0f : 1f;
            // Index the inspector list by enum value once, so Play is a direct array lookup.
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
