using BusJam.Level;
using TMPro;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>Tünelin görünümü: ağzı baktığı yöne döner, üstünde kalan yolcu sayısını gösterir.</summary>
    public class TunnelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text remainingText;

        private int lastRemaining = -1;

        public Vector2Int Cell { get; private set; }

        public void Setup(Vector2Int cell, Direction facing, int remaining)
        {
            Cell = cell;
            Vector2Int offset = facing.ToOffset();
            transform.localRotation = Quaternion.LookRotation(new Vector3(offset.x, 0f, -offset.y), Vector3.up);
            lastRemaining = -1;
            SetRemaining(remaining);
        }

        public void SetRemaining(int remaining)
        {
            if (remaining == lastRemaining)
                return;

            lastRemaining = remaining;
            remainingText.text = remaining.ToString();
        }
    }
}
