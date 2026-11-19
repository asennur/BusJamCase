using System;
using System.Collections.Generic;
using BusJam.Level;
using UnityEngine;

namespace BusJam.Gameplay
{
    /// <summary>
    /// Gridin oyun içi durumu: yürünebilirlik ve hücredeki yolcu. Yol ve erişilebilirlik BFS ile bulunur.
    /// Arama dizileri level kurulurken ayrılır, aramalarda tekrar kullanılır (allocation yok).
    /// </summary>
    public class Board
    {
        private static readonly Vector2Int[] Directions = { Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up };

        private bool[] walkable;
        private Passenger[] occupants;
        private int[] queue;
        private int[] parent;
        private bool[] visited;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int CellCount => Width * Height;

        public void Setup(LevelData level)
        {
            Width = level.width;
            Height = level.height;
            int count = CellCount;

            if (walkable == null || walkable.Length < count)
            {
                walkable = new bool[count];
                occupants = new Passenger[count];
                queue = new int[count];
                parent = new int[count];
                visited = new bool[count];
            }
            else
            {
                Array.Clear(occupants, 0, occupants.Length);
            }

            for (int i = 0; i < count; i++)
                walkable[i] = level.cells[i].IsWalkable;
        }

        public bool InBounds(Vector2Int cell) => cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        public int ToIndex(Vector2Int cell) => cell.y * Width + cell.x;
        public Vector2Int ToCell(int index) => new Vector2Int(index % Width, index / Width);

        public bool IsWalkable(Vector2Int cell) => InBounds(cell) && walkable[ToIndex(cell)];
        public Passenger GetOccupant(int index) => occupants[index];
        public Passenger GetOccupant(Vector2Int cell) => occupants[ToIndex(cell)];
        public void SetOccupant(Vector2Int cell, Passenger passenger) => occupants[ToIndex(cell)] = passenger;

        private bool IsFree(int index) => walkable[index] && occupants[index] == null;

        /// <summary>
        /// start'tan çıkış satırına (y = 0) boş hücrelerden en kısa yol. result start hariç, çıkış hücresi dahil;
        /// start zaten çıkış satırındaysa boş kalır ve true döner.
        /// </summary>
        public bool TryFindPath(Vector2Int start, List<Vector2Int> result)
        {
            result.Clear();
            if (GetOccupant(start) != null && GetOccupant(start).RopesLeft > 0)
                return false;
            if (start.y == 0)
                return true;

            Array.Clear(visited, 0, CellCount);
            int head = 0, tail = 0;
            int startIndex = ToIndex(start);
            visited[startIndex] = true;
            parent[startIndex] = -1;
            queue[tail++] = startIndex;

            while (head < tail)
            {
                int index = queue[head++];
                Vector2Int cell = ToCell(index);

                for (int d = 0; d < Directions.Length; d++)
                {
                    Vector2Int next = cell + Directions[d];
                    if (!InBounds(next))
                        continue;

                    int nextIndex = ToIndex(next);
                    if (visited[nextIndex] || !IsFree(nextIndex))
                        continue;

                    visited[nextIndex] = true;
                    parent[nextIndex] = index;

                    if (next.y == 0)
                    {
                        for (int i = nextIndex; i != startIndex; i = parent[i])
                            result.Add(ToCell(i));
                        result.Reverse();
                        return true;
                    }

                    queue[tail++] = nextIndex;
                }
            }

            return false;
        }

        /// <summary>
        /// Çıkış satırındaki boş hücrelerden tek flood-fill. Yolcu çıkış satırındaysa ya da ulaşılan
        /// boş bir hücreye komşuysa erişilebilir; result yalnız yolcu hücrelerinde true olur.
        /// </summary>
        public void ComputeReachable(bool[] result)
        {
            int count = CellCount;
            Array.Clear(visited, 0, count);
            Array.Clear(result, 0, count);
            int head = 0, tail = 0;

            for (int x = 0; x < Width; x++)
            {
                if (!walkable[x])
                    continue;

                if (occupants[x] != null)
                {
                    result[x] = occupants[x].RopesLeft == 0;
                    continue;
                }

                visited[x] = true;
                queue[tail++] = x;
            }

            while (head < tail)
            {
                Vector2Int cell = ToCell(queue[head++]);

                for (int d = 0; d < Directions.Length; d++)
                {
                    Vector2Int next = cell + Directions[d];
                    if (!InBounds(next))
                        continue;

                    int nextIndex = ToIndex(next);
                    if (visited[nextIndex] || !walkable[nextIndex])
                        continue;

                    if (occupants[nextIndex] != null)
                    {
                        result[nextIndex] = occupants[nextIndex].RopesLeft == 0;
                        continue;
                    }

                    visited[nextIndex] = true;
                    queue[tail++] = nextIndex;
                }
            }
        }
    }
}
