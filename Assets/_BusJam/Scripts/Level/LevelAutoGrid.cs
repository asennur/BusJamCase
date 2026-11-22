using System.Collections.Generic;
using UnityEngine;

namespace BusJam.Level
{
    /// <summary>
    /// Generates a game-like grid shape for the given size: a mirrored outline with void cut-outs, a few obstacles
    /// and (on larger boards) one tunnel. Row 0 is the exit row next to the buses and stays open. Every walkable
    /// tile is connected to the exit row. Passengers are cleared; LevelAutoFill places them afterwards.
    /// </summary>
    public static class LevelAutoGrid
    {
        public static bool Generate(LevelData level, out string message, int attempts = 60)
        {
            var random = new System.Random();
            int w = level.width, h = level.height;
            if (w < 2 || h < 2)
            {
                message = "Auto Grid needs at least a 2 x 2 grid.";
                return false;
            }

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                var cells = new CellData[w * h];
                var tunnels = new List<TunnelData>();
                CarveOutline(cells, w, h, random);
                PlaceObstacles(cells, w, h, random);
                if (w * h >= 20 && random.NextDouble() < 0.65) TryPlaceTunnel(cells, w, h, tunnels, random);

                if (CountEmpty(cells, tunnels, w) < 6 || !Connected(cells, w, h)) continue;

                level.cells = cells;
                level.tunnels = tunnels;
                message = $"Generated a {w} x {h} grid ({CountEmpty(cells, tunnels, w)} free tiles"
                    + (tunnels.Count > 0 ? ", 1 tunnel" : "") + (level.busOrder.Count > 0 ? "). Now Auto Fill." : "). Add buses, then Auto Fill.");
                return true;
            }

            message = "Could not generate a connected grid for this size. Try another size.";
            return false;
        }

        // Mirrored left/right outline: each row (except the exit row) is inset by a smoothly varying amount,
        // plus an optional hole in the middle of bigger boards.
        private static void CarveOutline(CellData[] cells, int w, int h, System.Random random)
        {
            int maxInset = Mathf.Max(0, (w - 3) / 2);
            int inset = 0;
            for (int y = 1; y < h; y++)
            {
                if (maxInset > 0)
                {
                    int step = random.Next(-1, 2);
                    inset = Mathf.Clamp(inset + step, 0, maxInset);
                }
                for (int i = 0; i < inset; i++)
                {
                    Set(cells, w, i, y, CellType.Void);
                    Set(cells, w, w - 1 - i, y, CellType.Void);
                }
            }

            if (w >= 5 && h >= 5 && random.NextDouble() < 0.4)
            {
                int cx = w / 2, cy = 2 + random.Next(Mathf.Max(1, h - 3));
                if (cy < h - 1)
                {
                    Set(cells, w, cx, cy, CellType.Void);
                    if (w % 2 == 0) Set(cells, w, cx - 1, cy, CellType.Void);
                }
            }
        }

        // 0-2 obstacles (mirrored as a pair when possible), never on the exit row.
        private static void PlaceObstacles(CellData[] cells, int w, int h, System.Random random)
        {
            int count = (w * h) >= 25 ? random.Next(0, 3) : random.Next(0, 2);
            for (int n = 0; n < count; n++)
            {
                int x = random.Next(w), y = 1 + random.Next(h - 1);
                if (Get(cells, w, x, y) != CellType.Empty) continue;
                Set(cells, w, x, y, CellType.Obstacle);
                int mirror = w - 1 - x;
                if (mirror != x && Get(cells, w, mirror, y) == CellType.Empty && random.NextDouble() < 0.6)
                    Set(cells, w, mirror, y, CellType.Obstacle);
            }
        }

        // A tunnel on a tile below the exit row whose mouth opens onto an empty tile.
        private static void TryPlaceTunnel(CellData[] cells, int w, int h, List<TunnelData> tunnels, System.Random random)
        {
            var dirs = new[] { Direction.Up, Direction.Left, Direction.Right, Direction.Down };
            for (int tries = 0; tries < 30; tries++)
            {
                int x = random.Next(w), y = 1 + random.Next(h - 1);
                if (Get(cells, w, x, y) != CellType.Empty) continue;
                foreach (var dir in dirs)
                {
                    var mouth = new Vector2Int(x, y) + dir.ToOffset();
                    if (mouth.x < 0 || mouth.y < 0 || mouth.x >= w || mouth.y >= h) continue;
                    if (Get(cells, w, mouth.x, mouth.y) != CellType.Empty) continue;
                    Set(cells, w, x, y, CellType.Tunnel);
                    tunnels.Add(new TunnelData { cell = new Vector2Int(x, y), facing = dir });
                    return;
                }
            }
        }

        private static int CountEmpty(CellData[] cells, List<TunnelData> tunnels, int w)
        {
            int count = 0;
            for (int i = 0; i < cells.Length; i++) if (cells[i].type == CellType.Empty) count++;
            return count - tunnels.Count; // the mouth is filled from the tunnel, not by Auto Fill
        }

        // Every walkable tile must reach the exit row (y = 0) through walkable tiles.
        private static bool Connected(CellData[] cells, int w, int h)
        {
            var seen = new bool[cells.Length];
            var queue = new Queue<int>();
            for (int x = 0; x < w; x++)
                if (cells[x].IsWalkable) { seen[x] = true; queue.Enqueue(x); }
            if (queue.Count == 0) return false;
            while (queue.Count > 0)
            {
                int i = queue.Dequeue(), cx = i % w, cy = i / w;
                Visit(cx - 1, cy); Visit(cx + 1, cy); Visit(cx, cy - 1); Visit(cx, cy + 1);
            }
            for (int i = 0; i < cells.Length; i++) if (cells[i].IsWalkable && !seen[i]) return false;
            return true;

            void Visit(int x, int y)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return;
                int i = y * w + x;
                if (seen[i] || !cells[i].IsWalkable) return;
                seen[i] = true;
                queue.Enqueue(i);
            }
        }

        private static CellType Get(CellData[] cells, int w, int x, int y) => cells[y * w + x].type;
        private static void Set(CellData[] cells, int w, int x, int y, CellType type) => cells[y * w + x] = new CellData { type = type };
    }
}
