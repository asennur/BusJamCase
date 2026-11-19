#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using BusJam.Core;
using BusJam.Level;
using UnityEngine;

namespace BusJam.LevelEditor
{
    /// <summary>Editöre özel, süre sınırı hariç mantıksal olaylarla DFS. Animasyonlar yerine varış olayları ilerletilir.</summary>
    public sealed class LevelSolver
    {
        public sealed class Result
        {
            public int SolutionCount;
            public bool Limited;
            public int States;
            public string Error;
            public readonly List<int[]> Solutions = new List<int[]>();
            public int[] LosingSequence;
            public string Label => Error != null ? Error : SolutionCount >= 2 ? "≥2 çözüm"
                : Limited ? "Sınır aşıldı (belirsiz)" : SolutionCount == 1 ? "Tek çözüm" : "Çözülemez";

            public string Describe(int width)
            {
                string text = $"{Label} · {States} durum";
                if (Solutions.Count > 0)
                    text += "\n" + Format(Solutions[0], width);
                return text;
            }
        }

        private struct Arrival
        {
            public int Color;
            public int Target; // -1 koltuk, -2 taşma, diğerleri slot
        }

        private sealed class State
        {
            public CellData[] Cells;
            public int[] TunnelOffsets;
            public int[] Slots; // 0 boş, pozitif hazır, negatif koşuyor; renk + 1
            public List<Arrival> Runs = new List<Arrival>();
            public List<int> Overflow = new List<int>();
            public int Bus, Reserved, Seated;
            public bool AtDoor;

            public State Copy() => new State
            {
                Cells = (CellData[])Cells.Clone(), TunnelOffsets = (int[])TunnelOffsets.Clone(),
                Slots = (int[])Slots.Clone(), Runs = new List<Arrival>(Runs), Overflow = new List<int>(Overflow),
                Bus = Bus, Reserved = Reserved, Seated = Seated, AtDoor = AtDoor
            };

            public string Key()
            {
                var key = new StringBuilder();
                key.Append(Bus).Append('/').Append(Reserved).Append('/').Append(Seated).Append('/').Append(AtDoor);
                foreach (var cell in Cells)
                    key.Append('|').Append((int)cell.type).Append(',').Append((int)cell.color)
                        .Append(',').Append(cell.ropes).Append(',').Append((int)cell.cloak);
                foreach (int offset in TunnelOffsets) key.Append('t').Append(offset);
                foreach (int slot in Slots) key.Append('s').Append(slot);
                foreach (var run in Runs) key.Append('r').Append(run.Color).Append(',').Append(run.Target);
                foreach (int color in Overflow) key.Append('o').Append(color);
                return key.ToString();
            }
        }

        private sealed class Paths
        {
            public readonly List<int[]> Wins = new List<int[]>();
            public int[] Loss;
        }

        private readonly Level.LevelData level;
        private readonly int slotCount, stateLimit, maxSolutions;
        private readonly double milliseconds;
        private readonly bool findLoss;
        private readonly Stopwatch watch = new Stopwatch();
        private readonly Dictionary<string, Paths> memo = new Dictionary<string, Paths>();
        private bool limited;
        private int states;

        private LevelSolver(Level.LevelData level, int levelNumber, int stateLimit, double milliseconds, bool findLoss, int maxSolutions)
        {
            this.level = level;
            this.maxSolutions = Mathf.Max(1, maxSolutions);
            this.stateLimit = stateLimit;
            this.milliseconds = milliseconds;
            this.findLoss = findLoss;
            for (int i = 0; i < GameRules.VisibleSlotCount; i++)
                if (GameRules.IsSlotUnlocked(i, levelNumber)) slotCount++;
        }

        public static Result Solve(Level.LevelData level, int levelNumber, int stateLimit = 200000,
            double milliseconds = 5000, bool findLosingSequence = false, int maxSolutions = 3)
        {
            if (!level.Validate(out string error)) return new Result { Error = "Geçersiz: " + error };
            var solver = new LevelSolver(level, levelNumber, stateLimit, milliseconds, findLosingSequence, maxSolutions);
            var start = new State
            {
                Cells = (Level.CellData[])level.cells.Clone(), Slots = new int[solver.slotCount],
                TunnelOffsets = new int[level.tunnels.Count], AtDoor = false
            };
            for (int i = 0; i < level.tunnels.Count; i++) solver.Emerge(start, i);
            solver.watch.Start();
            Paths paths = solver.Search(start);
            var result = new Result { SolutionCount = paths.Wins.Count, Limited = solver.limited,
                States = solver.states, LosingSequence = paths.Loss };
            result.Solutions.AddRange(paths.Wins);
            return result;
        }

        public static string SolveAll()
        {
            var db = UnityEditor.AssetDatabase.LoadAssetAtPath<Level.LevelDatabase>("Assets/_BusJam/Data/LevelDatabase.asset");
            var text = new StringBuilder("| Level | Sonuç | Durum | İlk çözüm (x,y) |\n| --- | --- | --- | --- |\n");
            for (int i = 0; i < db.Count; i++)
            {
                var level = db.Get(i);
                Result result = Solve(level, i + 1);
                text.Append('|').Append(level.name).Append('|').Append(result.Label).Append('|').Append(result.States)
                    .Append('|').Append(result.Solutions.Count > 0 ? Format(result.Solutions[0], level.width) : "—").AppendLine("|");
            }
            return text.ToString();
        }

        public static string Format(int[] sequence, int width)
        {
            var text = new StringBuilder();
            foreach (int index in sequence) text.Append('(').Append(index % width).Append(',').Append(index / width).Append(") ");
            return text.ToString().TrimEnd();
        }

        /// <summary>Dokunuşlar arasında tüm varışları bekleyerek bir diziyi yeniden yürütür.</summary>
        public static string ReplaySettled(LevelData level, int levelNumber, int[] sequence)
        {
            if (!level.Validate(out string error)) return error;
            var solver = new LevelSolver(level, levelNumber, 0, 0, false, 3);
            var state = new State { Cells = (CellData[])level.cells.Clone(), Slots = new int[solver.slotCount],
                TunnelOffsets = new int[level.tunnels.Count] };
            for (int i = 0; i < level.tunnels.Count; i++) solver.Emerge(state, i);
            foreach (int tap in sequence)
            {
                Settle();
                if (state.Bus == level.busOrder.Count) return "Complete sonrası dokunuş";
                if (Array.IndexOf(state.Slots, 0) < 0) return "NoSpace";
                if (tap < 0 || tap >= state.Cells.Length) return "Grid dışında";
                var cell = state.Cells[tap];
                if (cell.type != CellType.Passenger || cell.ropes > 0 || cell.cloak == Cloak.StartsClosed
                    || !solver.Reachable(state)[tap] || !solver.Tap(state, tap)) return "Reddedilen dokunuş";
            }
            Settle();
            if (state.Bus == level.busOrder.Count) return "Complete";
            if (Array.IndexOf(state.Slots, 0) < 0) return "NoSpace";
            bool[] reachable = solver.Reachable(state);
            for (int i = 0; i < state.Cells.Length; i++)
            {
                var cell = state.Cells[i];
                if (cell.type == CellType.Passenger && reachable[i] && cell.ropes == 0 && cell.cloak != Cloak.StartsClosed)
                    return "Playing";
            }
            return "NoMoves";

            void Settle()
            {
                while (state.Bus < level.busOrder.Count && (!state.AtDoor || state.Runs.Count > 0)) solver.Advance(state);
            }
        }

        private Paths Search(State state)
        {
            var result = new Paths();
            if (states >= stateLimit || watch.Elapsed.TotalMilliseconds >= milliseconds)
            {
                limited = true;
                return result;
            }
            string key = state.Key();
            if (memo.TryGetValue(key, out var cached)) return cached;
            states++;
            if (state.Bus == level.busOrder.Count)
                result.Wins.Add(Array.Empty<int>());
            else
            {
                bool moving = state.Runs.Count > 0 || !state.AtDoor;
                if (moving)
                {
                    var next = state.Copy();
                    Advance(next);
                    Merge(result, Search(next), -1);
                }
                // Dolu, tamamen durmuş slotlar ZD2'de gridin renginden bağımsız fail'dir.
                if (moving || Array.IndexOf(state.Slots, 0) >= 0)
                {
                    bool[] reachable = Reachable(state);
                    for (int i = 0; i < state.Cells.Length && !Enough(result) && !limited; i++)
                    {
                        var cell = state.Cells[i];
                        if (cell.type != Level.CellType.Passenger || !reachable[i] || cell.ropes > 0
                            || cell.cloak == Level.Cloak.StartsClosed) continue;
                        var next = state.Copy();
                        if (Tap(next, i)) Merge(result, Search(next), i);
                    }
                }
                if (!moving && result.Wins.Count == 0 && result.Loss == null && !limited)
                    result.Loss = Array.Empty<int>();
            }
            // Sınır nedeniyle yarım kalan arama kesin sonuç olarak memo'ya yazılmaz.
            if (!limited) memo[key] = result;
            return result;
        }

        private bool Enough(Paths paths) => paths.Wins.Count >= maxSolutions && (!findLoss || paths.Loss != null);

        private void Merge(Paths result, Paths child, int tap)
        {
            foreach (int[] suffix in child.Wins)
            {
                int[] sequence = Prepend(tap, suffix);
                bool duplicate = false;
                foreach (int[] existing in result.Wins)
                {
                    if (existing.Length != sequence.Length) continue;
                    bool equal = true;
                    for (int i = 0; i < existing.Length; i++) if (existing[i] != sequence[i]) { equal = false; break; }
                    if (equal) { duplicate = true; break; }
                }
                if (!duplicate && result.Wins.Count < maxSolutions) result.Wins.Add(sequence);
            }
            if (result.Loss == null && child.Loss != null) result.Loss = Prepend(tap, child.Loss);
        }

        private static int[] Prepend(int tap, int[] suffix)
        {
            if (tap < 0) return suffix;
            var sequence = new int[suffix.Length + 1];
            sequence[0] = tap;
            Array.Copy(suffix, 0, sequence, 1, suffix.Length);
            return sequence;
        }

        private bool Tap(State state, int index)
        {
            var cell = state.Cells[index];
            int slot = Array.IndexOf(state.Slots, 0);
            bool direct = slot >= 0 && state.AtDoor && state.Reserved < GameRules.BusCapacity
                && level.busOrder[state.Bus] == cell.color;
            if (slot < 0 && !SlotWillOpen(state)) return false;
            state.Runs.Add(new Arrival { Color = (int)cell.color, Target = direct ? -1 : slot >= 0 ? slot : -2 });
            if (direct) state.Reserved++;
            else if (slot >= 0) state.Slots[slot] = -((int)cell.color + 1);
            state.Cells[index] = new Level.CellData { type = Level.CellType.Empty };
            foreach (int neighbor in Neighbors(index))
                if (state.Cells[neighbor].type == Level.CellType.Passenger && state.Cells[neighbor].ropes > 0)
                    state.Cells[neighbor].ropes--;
            for (int t = 0; t < level.tunnels.Count; t++)
                if (Mouth(t) == index) { Emerge(state, t); break; }
            for (int i = 0; i < state.Cells.Length; i++)
                if (state.Cells[i].type == Level.CellType.Passenger && state.Cells[i].cloak != Level.Cloak.None)
                    state.Cells[i].cloak = state.Cells[i].cloak == Level.Cloak.StartsOpen
                        ? Level.Cloak.StartsClosed : Level.Cloak.StartsOpen;
            return true;
        }

        private bool SlotWillOpen(State state)
        {
            int bus = state.Bus + (state.Reserved == GameRules.BusCapacity ? 1 : 0);
            if (bus >= level.busOrder.Count) return false;
            foreach (int slot in state.Slots)
                if (Math.Abs(slot) == (int)level.busOrder[bus] + 1) return true;
            return false;
        }

        private void Advance(State state)
        {
            if (!state.AtDoor)
            {
                state.AtDoor = true;
                BoardSlots(state);
                return;
            }
            Arrival run = state.Runs[0];
            state.Runs.RemoveAt(0);
            if (run.Target == -1)
            {
                state.Seated++;
                if (state.Seated == GameRules.BusCapacity)
                {
                    state.Bus++;
                    state.Reserved = state.Seated = 0;
                    state.AtDoor = false;
                }
            }
            else if (run.Target >= 0)
                state.Slots[run.Target] = run.Color + 1;
            else
                state.Overflow.Add(run.Color);
            if (state.Bus < level.busOrder.Count && state.AtDoor) BoardSlots(state);
        }

        private void BoardSlots(State state)
        {
            for (int i = 0; i < state.Slots.Length && state.Reserved < GameRules.BusCapacity; i++)
            {
                if (state.Slots[i] != (int)level.busOrder[state.Bus] + 1) continue;
                state.Runs.Add(new Arrival { Color = state.Slots[i] - 1, Target = -1 });
                state.Reserved++;
                state.Slots[i] = 0;
                // FIFO taşma: hazır yolcu ya da halen koşan ilk taşma yolcusu bu slota atanır.
                if (state.Overflow.Count > 0)
                {
                    int color = state.Overflow[0];
                    state.Overflow.RemoveAt(0);
                    state.Slots[i] = -(color + 1);
                    state.Runs.Add(new Arrival { Color = color, Target = i });
                }
                else
                    for (int r = 0; r < state.Runs.Count; r++)
                        if (state.Runs[r].Target == -2)
                        {
                            var run = state.Runs[r]; run.Target = i; state.Runs[r] = run;
                            state.Slots[i] = -(run.Color + 1); break;
                        }
            }
        }

        private int Mouth(int tunnel) => level.tunnels[tunnel].MouthCell.y * level.width + level.tunnels[tunnel].MouthCell.x;

        private void Emerge(State state, int tunnel)
        {
            int mouth = Mouth(tunnel);
            var queue = level.tunnels[tunnel].queue;
            if (state.Cells[mouth].type == Level.CellType.Passenger || state.TunnelOffsets[tunnel] == queue.Count) return;
            state.Cells[mouth] = new Level.CellData { type = Level.CellType.Passenger, color = queue[state.TunnelOffsets[tunnel]++] };
        }

        private IEnumerable<int> Neighbors(int index)
        {
            int x = index % level.width;
            if (x > 0) yield return index - 1;
            if (x + 1 < level.width) yield return index + 1;
            if (index >= level.width) yield return index - level.width;
            if (index + level.width < level.CellCount) yield return index + level.width;
        }

        private bool[] Reachable(State state)
        {
            var reachable = new bool[level.CellCount];
            var visited = new bool[level.CellCount];
            var queue = new Queue<int>();
            for (int i = 0; i < level.width; i++) Visit(i);
            while (queue.Count > 0)
                foreach (int neighbor in Neighbors(queue.Dequeue())) Visit(neighbor);
            return reachable;

            void Visit(int index)
            {
                if (visited[index] || !state.Cells[index].IsWalkable) return;
                visited[index] = true;
                if (state.Cells[index].type == Level.CellType.Passenger) reachable[index] = true;
                else queue.Enqueue(index);
            }
        }
    }
}
#endif
