using BusJam.Core;
using TMPro;
using UnityEngine;

namespace BusJam.UI
{
    /// <summary>Updates life counts and countdown text only when their displayed values change.</summary>
    public sealed class LivesDisplay : MonoBehaviour
    {
        [SerializeField] private LivesSystem livesSystem;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private GameObject countdownGroup;
        [SerializeField] private bool showNextLifeLabel;
        [SerializeField] private bool showFull;

        private readonly char[] countBuffer = new char[10];
        private readonly char[] timeBuffer = new char[32];
        private int lastCount = -1;
        private int lastSeconds = -1;

        private void OnEnable()
        {
            livesSystem.Changed += Show;
            lastCount = lastSeconds = -1;
            livesSystem.Refresh();
            Show(livesSystem.Lives, livesSystem.NextLifeSeconds);
        }

        private void OnDisable() => livesSystem.Changed -= Show;

        private void Show(int lives, int seconds)
        {
            if (lastCount != lives)
            {
                lastCount = lives;
                if (countText != null)
                {
                    int length = WriteNumber(countBuffer, 0, lives, 1);
                    countText.SetCharArray(countBuffer, 0, length);
                }
            }
            bool waiting = lives < livesSystem.MaxLives;
            bool visible = waiting || showFull;
            if (countdownGroup != null && countdownGroup.activeSelf != visible) countdownGroup.SetActive(visible);
            if (!waiting && showFull && countdownText != null) countdownText.SetText("FULL");
            if (lastSeconds == seconds) return;
            lastSeconds = seconds;
            if (!waiting || countdownText == null) return;
            int offset = 0;
            if (showNextLifeLabel)
            {
                const string prefix = "Next life in ";
                prefix.CopyTo(0, timeBuffer, 0, prefix.Length);
                offset = prefix.Length;
            }
            offset = WriteNumber(timeBuffer, offset, seconds / 60, 2);
            timeBuffer[offset++] = ':';
            offset = WriteNumber(timeBuffer, offset, seconds % 60, 2);
            countdownText.SetCharArray(timeBuffer, 0, offset);
        }

        private static int WriteNumber(char[] buffer, int offset, int value, int minDigits)
        {
            int digits = 1;
            for (int n = value; n >= 10; n /= 10) digits++;
            digits = Mathf.Max(digits, minDigits);
            int end = offset + digits;
            for (int i = end - 1; i >= offset; i--)
            {
                buffer[i] = (char)('0' + value % 10);
                value /= 10;
            }
            return end;
        }
    }
}
