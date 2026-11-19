using System;
using System.Globalization;
using UnityEngine;

namespace BusJam.Core
{
    /// <summary>Persists lives and recovers elapsed UTC time, including time spent offline.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class LivesSystem : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxLives = 5;
        [SerializeField, Min(1)] private int regenMinutes = 20;

        private const string LivesKey = "Lives";
        private const string RegenKey = "LastRegenUtc";
        private const string GuardKey = "LivesUtcHighWater";
        private long lastRegenUtc;
        private long lastObservedUtc;
        private long rollbackUntilUtc;
        private bool initialized;
        private int publishedLives = -1;
        private int publishedSeconds = -1;

        public int MaxLives => maxLives;
        public int Lives { get; private set; }
        public int NextLifeSeconds { get; private set; }
        public event Action<int, int> Changed;

        private long IntervalTicks => (long)Mathf.Max(1, regenMinutes) * TimeSpan.TicksPerMinute;

        private void Awake() => Initialize(DateTime.UtcNow.Ticks);

        private void OnEnable()
        {
            Refresh();
            InvokeRepeating(nameof(Refresh), 1f, 1f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Refresh));
            if (initialized) Save();
        }

        private void OnApplicationPause(bool paused)
        {
            Refresh();
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Refresh();
            Save();
        }

        private void Initialize(long now)
        {
            if (initialized) return;
            initialized = true;
            maxLives = Mathf.Max(1, maxLives);
            Lives = Mathf.Clamp(PlayerPrefs.GetInt(LivesKey, maxLives), 0, maxLives);
            lastRegenUtc = ReadTicks(RegenKey, now);
            long guard = ReadTicks(GuardKey, now);
            lastObservedUtc = now;
            rollbackUntilUtc = guard > now ? guard : 0;
            // A future saved guard must not overwrite valid progress on every startup.
            if (lastRegenUtc > now) lastRegenUtc = now;
            RefreshAt(now);
            Save();
        }

        private static long ReadTicks(string key, long fallback)
        {
            return long.TryParse(PlayerPrefs.GetString(key, ""), NumberStyles.None,
                CultureInfo.InvariantCulture, out long ticks) && ticks > 0 && ticks <= DateTime.MaxValue.Ticks
                ? ticks : fallback;
        }

        public void Refresh()
        {
            long now = DateTime.UtcNow.Ticks;
            if (!initialized) Initialize(now);
            else RefreshAt(now);
        }

        // There is no server clock; a real game uses server time.
        private void RefreshAt(long now)
        {
            bool save = false;
            if (now < lastObservedUtc)
            {
                rollbackUntilUtc = Math.Max(rollbackUntilUtc, Math.Max(lastObservedUtc, lastRegenUtc));
                lastRegenUtc = now;
                save = true;
            }
            else if (rollbackUntilUtc > 0 && now >= rollbackUntilUtc)
            {
                // Crossing the old clock value must not award the forward rebound.
                lastRegenUtc = now;
                rollbackUntilUtc = 0;
                save = true;
            }
            else if (Lives >= maxLives)
            {
                lastRegenUtc = now;
            }
            else
            {
                long earned = (now - lastRegenUtc) / IntervalTicks;
                if (earned > 0)
                {
                    Lives += (int)Math.Min(earned, maxLives - Lives);
                    lastRegenUtc = Lives == maxLives ? now : lastRegenUtc + earned * IntervalTicks;
                    save = true;
                }
            }

            lastObservedUtc = now;
            NextLifeSeconds = Lives >= maxLives ? 0 : (int)((IntervalTicks - (now - lastRegenUtc)
                + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond);
            if (save) Save();
            Publish();
        }

        public bool TrySpendLife()
        {
            Refresh();
            if (Lives == 0) return false;
            if (Lives == maxLives) lastRegenUtc = lastObservedUtc;
            Lives--;
            NextLifeSeconds = (int)((IntervalTicks - (lastObservedUtc - lastRegenUtc)
                + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond);
            Save();
            Publish();
            return true;
        }

        /// <summary>Refills lives and resets the regeneration clock after a confirmed progress reset.</summary>
        public void ResetFull()
        {
            initialized = true;
            Lives = maxLives;
            lastRegenUtc = lastObservedUtc = DateTime.UtcNow.Ticks;
            rollbackUntilUtc = 0;
            NextLifeSeconds = 0;
            Save();
            Publish();
        }

        private void Publish()
        {
            if (publishedLives == Lives && publishedSeconds == NextLifeSeconds) return;
            publishedLives = Lives;
            publishedSeconds = NextLifeSeconds;
            Changed?.Invoke(Lives, NextLifeSeconds);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(LivesKey, Lives);
            PlayerPrefs.SetString(RegenKey, lastRegenUtc.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(GuardKey, Math.Max(lastObservedUtc, rollbackUntilUtc).ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
    }
}
