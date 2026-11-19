#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using BusJam.LevelEditor;
using UnityEngine;

namespace BusJam.Level
{
    /// <summary>
    /// Fills the passengers a level still needs (3 per bus, minus passengers already placed or queued in tunnels)
    /// into free tiles, retrying random layouts until the solver finds at least two solutions.
    /// The designer keeps control of the grid shape, tunnels, bus order and any passenger already placed.
    /// </summary>
    public static class LevelAutoFill
    {
        public const int SeatsPerBus = 3;

        public static bool Fill(LevelData level, int levelNumber, out string message, int attempts = 40)
        {
            var free = FreeCells(level);
            if (level.busOrder == null) level.busOrder = new List<PassengerColor>();
            if (level.busOrder.Count == 0)
                CreateBusOrder(level, free.Count);

            var needed = new List<PassengerColor>();
            foreach (PassengerColor color in Enum.GetValues(typeof(PassengerColor)))
            {
                int missing = Required(level, color) - Placed(level, color);
                if (missing < 0)
                {
                    message = $"Too many {color} passengers: remove {-missing} or add a {color} bus.";
                    return false;
                }
                for (int i = 0; i < missing; i++) needed.Add(color);
            }

            if (needed.Count == 0)
            {
                message = "Nothing to fill: every bus already has its passengers.";
                return false;
            }
            if (needed.Count > free.Count)
            {
                message = $"Not enough free tiles: {needed.Count} passengers need a place, {free.Count} tiles are free.";
                return false;
            }

            CellData[] original = (CellData[])level.cells.Clone();
            CellData[] best = null;
            int bestCount = 0;
            var random = new System.Random();
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                var cells = (CellData[])original.Clone();
                Shuffle(free, random);
                Shuffle(needed, random);
                for (int i = 0; i < needed.Count; i++)
                    cells[free[i]] = new CellData { type = CellType.Passenger, color = needed[i] };

                level.cells = cells;
                var result = LevelSolver.Solve(level, levelNumber, 60000, 1200, false, 2);
                if (result.SolutionCount > bestCount)
                {
                    bestCount = result.SolutionCount;
                    best = cells;
                }
                if (bestCount >= 2) break;
            }

            if (best == null)
            {
                level.cells = original;
                message = $"No solvable layout found in {attempts} tries. Change the grid or the bus order and try again.";
                return false;
            }

            level.cells = best;
            message = bestCount >= 2
                ? $"Filled {needed.Count} passengers. The level has at least 2 solutions."
                : $"Filled {needed.Count} passengers, but only one solution was found. Try again for a looser layout.";
            return true;
        }

        public static int Required(LevelData level, PassengerColor color)
        {
            int buses = 0;
            foreach (var busColor in level.busOrder)
                if (busColor == color) buses++;
            return buses * SeatsPerBus;
        }

        public static int Placed(LevelData level, PassengerColor color)
        {
            int count = 0;
            foreach (var cell in level.cells)
                if (cell.type == CellType.Passenger && cell.color == color) count++;
            foreach (var tunnel in level.tunnels)
                foreach (var queued in tunnel.queue)
                    if (queued == color) count++;
            return count;
        }

        // Empty walkable tiles that are not a tunnel mouth (the mouth is filled from the tunnel at start).
        private static List<int> FreeCells(LevelData level)
        {
            var mouths = new HashSet<Vector2Int>();
            foreach (var tunnel in level.tunnels) mouths.Add(tunnel.MouthCell);
            var free = new List<int>();
            for (int y = 0; y < level.height; y++)
                for (int x = 0; x < level.width; x++)
                    if (level.GetCell(x, y).type == CellType.Empty && !mouths.Contains(new Vector2Int(x, y)))
                        free.Add(y * level.width + x);
            return free;
        }

        // No bus order given: one bus per three free tiles, colours spread so neighbouring buses differ.
        private static void CreateBusOrder(LevelData level, int freeTiles)
        {
            var colors = (PassengerColor[])Enum.GetValues(typeof(PassengerColor));
            int buses = Mathf.Clamp(freeTiles / SeatsPerBus, 1, 12);
            var random = new System.Random();
            int previous = -1;
            for (int i = 0; i < buses; i++)
            {
                int pick;
                do pick = random.Next(Mathf.Min(colors.Length, 2 + buses / 2)); while (pick == previous && buses > 1);
                level.busOrder.Add(colors[pick]);
                previous = pick;
            }
        }

        private static void Shuffle<T>(IList<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
#endif
