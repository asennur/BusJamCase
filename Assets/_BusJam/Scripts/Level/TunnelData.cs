using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam.Level
{
    /// <summary>Tünelin baktığı yön; ağız hücresi bu yöndeki komşudur. Up/Down grid y'sini azaltır/artırır (y = 0 çıkışa yakın).</summary>
    public enum Direction : byte
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class DirectionExtensions
    {
        public static Vector2Int ToOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return new Vector2Int(0, -1);
                case Direction.Down: return new Vector2Int(0, 1);
                case Direction.Left: return new Vector2Int(-1, 0);
                default: return new Vector2Int(1, 0);
            }
        }
    }

    /// <summary>Bir tünelin yeri, baktığı yön ve sırayla çıkacak yolcu kuyruğu. Hücre tipi Tunnel olur.</summary>
    [Serializable]
    public class TunnelData
    {
        public Vector2Int cell;
        public Direction facing;
        public List<PassengerColor> queue = new List<PassengerColor>();

        public Vector2Int MouthCell => cell + facing.ToOffset();
    }
}
