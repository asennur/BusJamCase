using System;
using System.Collections.Generic;
using System.Text;
using BusJam.Core;
using BusJam.Level;
using BusJam.LevelEditor;
using UnityEditor;
using UnityEngine;

namespace BusJam.EditorTools
{
    /// <summary>
    /// Yalnız editör toplu aracı: 12-25 arası levelleri bir zorluk eğrisinden, Level Editor'ün kullandığı araçlarla kurar
    /// (LevelAutoGrid → LevelAutoFill → mekanikler); level yalnız doğrulanırsa ve solver en az iki çözüm bulursa
    /// tutulur, sonra kaydedilip LevelDatabase'e eklenir.
    /// </summary>
    public static class LevelBatchGenerator
    {
        private const string DatabasePath = "Assets/_BusJam/Data/LevelDatabase.asset";
        private const string LevelFolder = "Assets/_BusJam/Data/Levels";

        // Zorluk eğrisinin bir satırı.
        private struct Band
        {
            public int First, Last, Width, Height, MinBuses, MaxBuses, Colors, Hidden, Ropes, Cloaks;
            public Band(int first, int last, int width, int height, int minBuses, int maxBuses, int colors, int hidden, int ropes, int cloaks)
            {
                First = first; Last = last; Width = width; Height = height; MinBuses = minBuses; MaxBuses = maxBuses;
                Colors = colors; Hidden = hidden; Ropes = ropes; Cloaks = cloaks;
            }
        }

        // Grid, otobüs sayısı, renkler ve mekanikler adım adım büyür; level 17 daha hafif bir nefes alma levelidir.
        private static readonly Band[] Curve =
        {
            new Band(12, 14, 5, 6, 5, 6, 3, 1, 0, 0),
            new Band(15, 16, 6, 7, 6, 7, 4, 2, 0, 0),
            new Band(17, 17, 5, 6, 5, 5, 3, 0, 1, 0),
            new Band(18, 20, 6, 7, 7, 8, 4, 1, 2, 0),
            new Band(21, 23, 7, 8, 8, 9, 5, 1, 1, 2),
            new Band(24, 25, 7, 8, 9, 10, 5, 1, 2, 2),
        };

        [MenuItem("Bus Jam/Levels/Generate Levels 12-25")]
        public static void GenerateMenu() => Debug.Log(Generate());

        public static string Generate(int seed = 2026)
        {
            var database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            var random = new System.Random(seed);
            var report = new StringBuilder("| Level | Grid | Buses | Mechanics | Solutions |\n| --- | --- | --- | --- | --- |\n");

            foreach (var band in Curve)
            {
                for (int number = band.First; number <= band.Last; number++)
                {
                    if (database.Count >= number) { report.AppendLine($"| {number} | exists, skipped | | | |"); continue; }
                    LevelData level = Build(band, number, random, out int solutions, out string mechanics);
                    if (level == null) { report.AppendLine($"| {number} | FAILED after all tries | | | |"); break; }

                    string path = $"{LevelFolder}/Level_{number:00}.asset";
                    level.name = $"Level_{number:00}";
                    AssetDatabase.CreateAsset(level, path);
                    database.Add(level);
                    report.AppendLine($"| {number} | {level.width}x{level.height} | {level.busOrder.Count} | {mechanics} | {(solutions >= 2 ? "2+" : solutions.ToString())} |");
                }
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private static LevelData Build(Band band, int number, System.Random random, out int solutions, out string mechanics)
        {
            solutions = 0;
            mechanics = "";
            for (int attempt = 0; attempt < 25; attempt++)
            {
                var level = ScriptableObject.CreateInstance<LevelData>();
                level.Resize(band.Width, band.Height);
                level.busOrder = BusOrder(random.Next(band.MinBuses, band.MaxBuses + 1), band.Colors, random);
                level.timeLimit = 50 + level.busOrder.Count * 6;

                if (!LevelAutoGrid.Generate(level, out _) || !LevelAutoFill.Fill(level, number, out _, 15))
                {
                    UnityEngine.Object.DestroyImmediate(level);
                    continue;
                }

                mechanics = AddMechanics(level, band, random);
                if (level.Validate(out _))
                {
                    var result = LevelSolver.Solve(level, number, 150000, 3000, false, 2);
                    if (result.SolutionCount >= 2)
                    {
                        solutions = result.SolutionCount;
                        return level;
                    }
                }
                UnityEngine.Object.DestroyImmediate(level);
            }
            return null;
        }

        // Karıştırılmış paletin ilk N renginden rastgele otobüs sırası; aynı renk hiç art arda gelmez.
        private static List<PassengerColor> BusOrder(int count, int colorCount, System.Random random)
        {
            var palette = new List<PassengerColor>(LevelEditorPanel.Colors);
            for (int i = palette.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                PassengerColor temp = palette[i];
                palette[i] = palette[j];
                palette[j] = temp;
            }
            colorCount = Mathf.Min(colorCount, palette.Count);

            var order = new List<PassengerColor>();
            for (int i = 0; i < count; i++)
            {
                PassengerColor pick;
                do pick = palette[random.Next(colorCount)];
                while (order.Count > 0 && pick == order[order.Count - 1]);
                order.Add(pick);
            }
            return order;
        }

        // Yerleşik yolculardan bazılarını gizli, ipli ya da pelerinli yapar. İp yalnız yeterli komşunun
        // ayrılabildiği yere konur (LevelData.Validate'in baktığı kural); bir yolcuya hiç iki mekanik verilmez.
        private static string AddMechanics(LevelData level, Band band, System.Random random)
        {
            var passengers = new List<int>();
            for (int i = 0; i < level.cells.Length; i++)
                if (level.cells[i].type == CellType.Passenger) passengers.Add(i);
            for (int i = passengers.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = passengers[i];
                passengers[i] = passengers[j];
                passengers[j] = temp;
            }

            int hidden = 0, ropes = 0, cloaks = 0;
            foreach (int index in passengers)
            {
                CellData cell = level.cells[index];
                int y = index / level.width;
                if (y == 0) continue; // her levelde kolay bir ilk hamle olsun diye çıkış satırı sade kalır

                if (ropes < band.Ropes)
                {
                    int free = PassengerNeighbours(level, index);
                    if (free >= 1)
                    {
                        cell.ropes = (byte)Mathf.Min(Mathf.Min(free, GameRules.MaxRopes), 1 + random.Next(2));
                        level.cells[index] = cell;
                        ropes++;
                        continue;
                    }
                }
                if (cloaks < band.Cloaks)
                {
                    cell.cloak = cloaks % 2 == 0 ? Cloak.StartsOpen : Cloak.StartsClosed;
                    level.cells[index] = cell;
                    cloaks++;
                    continue;
                }
                if (hidden < band.Hidden)
                {
                    cell.hidden = true;
                    level.cells[index] = cell;
                    hidden++;
                }
            }

            var parts = new List<string>();
            if (hidden > 0) parts.Add($"{hidden} hidden");
            if (ropes > 0) parts.Add($"{ropes} rope");
            if (cloaks > 0) parts.Add($"{cloaks} cloak");
            if (level.tunnels.Count > 0) parts.Add("tunnel");
            return parts.Count == 0 ? "plain" : string.Join(", ", parts);
        }

        // Yolcu olan komşular (ayrılabilir ve her biri bir ip çözer).
        private static int PassengerNeighbours(LevelData level, int index)
        {
            int x = index % level.width, y = index / level.width, count = 0;
            if (x > 0 && level.GetCell(x - 1, y).type == CellType.Passenger) count++;
            if (x < level.width - 1 && level.GetCell(x + 1, y).type == CellType.Passenger) count++;
            if (y > 0 && level.GetCell(x, y - 1).type == CellType.Passenger) count++;
            if (y < level.height - 1 && level.GetCell(x, y + 1).type == CellType.Passenger) count++;
            return count;
        }
    }
}
