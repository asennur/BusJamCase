using System;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Geri sayım; yalnız Playing'de çalışır. Görünen saniye değişince TimeChanged, 0'da Expired.</summary>
    public class LevelTimer : MonoBehaviour
    {
        private float remaining;
        private int lastSecond;

        public int SecondsRemaining => lastSecond;

        public event Action<int> TimeChanged;
        public event Action Expired;

        private void Update()
        {
            remaining -= Time.deltaTime;

            if (remaining <= 0f)
            {
                remaining = 0f;
                enabled = false;
                UpdateDisplay();
                Expired?.Invoke();
                return;
            }

            UpdateDisplay();
        }

        public void StartTimer(float duration)
        {
            PrepareTimer(duration);
            enabled = true;
        }

        public void PrepareTimer(float duration)
        {
            remaining = duration;
            lastSecond = -1;
            enabled = false;
            UpdateDisplay();
        }

        public void StopTimer() => enabled = false;

        private void UpdateDisplay()
        {
            int second = Mathf.CeilToInt(remaining);
            if (second == lastSecond)
                return;

            lastSecond = second;
            TimeChanged?.Invoke(second);
        }
    }
}
