using System.Collections.Generic;
using BusJam.Core;
using UnityEngine;

namespace BusJam.Level
{
    /// <summary>
    /// Bir levelin tüm verisi. Oyun sahnesi okur, Level Editor yazar.
    /// Hücreler satır satır tutulur; y = 0 otobüse en yakın (çıkış) satırıdır.
    /// </summary>
    [CreateAssetMenu(menuName = "BusJam/Level Data", fileName = "Level_")]
    public class LevelData : ScriptableObject
    {
        [Min(1)] public int width = 3;
        [Min(1)] public int height = 3;
        [Min(1f)] public float timeLimit = 60f;
        public CellData[] cells = new CellData[9];
        public List<PassengerColor> busOrder = new List<PassengerColor>();
        public List<TunnelData> tunnels = new List<TunnelData>();

        public int CellCount => width * height;

        public CellData GetCell(int x, int y) => cells[y * width + x];

        public void SetCell(int x, int y, CellData cell) => cells[y * width + x] = cell;

        /// <summary>Boyut değişince örtüşen hücreleri koruyarak diziyi yeniden kurar.</summary>
        public void Resize(int newWidth, int newHeight)
        {
            var resized = new CellData[newWidth * newHeight];
            int w = Mathf.Min(width, newWidth);
            int h = Mathf.Min(height, newHeight);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    resized[y * newWidth + x] = cells[y * width + x];

            width = newWidth;
            height = newHeight;
            cells = resized;
            tunnels.RemoveAll(tunnel => tunnel.cell.x >= width || tunnel.cell.y >= height);
        }

        /// <summary>
        /// Leveli kaydetmeden önce kurallara uyuyor mu bakar: en az bir otobüs, renk başına yolcu = otobüs × kapasite,
        /// çıkış satırında yürünebilir hücre var ve tüm yürünebilir hücreler boş gridde çıkışa bağlı.
        /// </summary>
        public bool Validate(out string error)
        {
            if (cells == null || cells.Length != CellCount)
            {
                error = $"Hücre sayısı {cells?.Length ?? 0}, beklenen {CellCount}";
                return false;
            }

            if (busOrder == null || busOrder.Count == 0)
            {
                error = "Otobüs sırası boş";
                return false;
            }

            if (width > GameRules.MaxWidth || height > GameRules.MaxHeight)
            {
                error = $"Grid {width}x{height}, izin verilen en fazla {GameRules.MaxWidth}x{GameRules.MaxHeight}";
                return false;
            }

            return ValidateCloaks(out error) && ValidateTunnels(out error) && ValidateRopes(out error)
                && ValidateColorCounts(out error) && ValidateConnectivity(out error);
        }

        private bool ValidateCloaks(out string error)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                CellData cell = cells[i];
                if (cell.cloak == Cloak.None)
                    continue;

                string reason = cell.cloak > Cloak.StartsClosed ? "Geçersiz pelerin değeri"
                    : cell.type != CellType.Passenger ? "Pelerin yalnız Passenger hücresinde olabilir"
                    : cell.hidden ? "Pelerin ve gizli yolcu (hidden) birlikte kullanılamaz" : null;
                if (reason != null)
                {
                    error = $"({i % width},{i / width}) {reason}";
                    return false;
                }
            }

            error = null;
            return true;
        }

        // Her komşu en fazla bir kez sayılır; dolu tünel ağzı başlangıçta kuyruktan da dolabilir.
        private bool ValidateRopes(out string error)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                CellData cell = cells[i];
                if (cell.ropes == 0)
                    continue;

                int x = i % width;
                int y = i / width;
                if (cell.type != CellType.Passenger)
                {
                    error = $"({x},{y}) ip yalnız Passenger hücresinde olabilir";
                    return false;
                }

                int neighbors = (CanVacate(x - 1, y) ? 1 : 0) + (CanVacate(x + 1, y) ? 1 : 0)
                    + (CanVacate(x, y - 1) ? 1 : 0) + (CanVacate(x, y + 1) ? 1 : 0);
                if (cell.ropes > neighbors)
                {
                    error = $"({x},{y}) ipli yolcu hiç çözülemez: {cell.ropes} ip, {neighbors} dolu komşu";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private bool CanVacate(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;

            CellData cell = GetCell(x, y);
            if (cell.type == CellType.Passenger)
                return true;
            if (!cell.IsWalkable)
                return false;

            var position = new Vector2Int(x, y);
            for (int i = 0; i < tunnels.Count; i++)
                if (tunnels[i].MouthCell == position && tunnels[i].queue.Count > 0)
                    return true;
            return false;
        }

        private bool ValidateColorCounts(out string error)
        {
            int colorCount = System.Enum.GetValues(typeof(PassengerColor)).Length;
            var passengers = new int[colorCount];
            var seats = new int[colorCount];

            foreach (var cell in cells)
                if (cell.type == CellType.Passenger)
                    passengers[(int)cell.color]++;

            foreach (var tunnel in tunnels)
                foreach (var color in tunnel.queue)
                    passengers[(int)color]++;

            foreach (var color in busOrder)
                seats[(int)color] += GameRules.BusCapacity;

            for (int i = 0; i < colorCount; i++)
            {
                if (passengers[i] != seats[i])
                {
                    error = $"{(PassengerColor)i}: {passengers[i]} yolcu, {seats[i]} koltuk";
                    return false;
                }
            }

            error = null;
            return true;
        }

        // Her tünelin ağzı grid içinde ve yürünebilir bir hücre (Void/Tunnel olamaz) olmalı.
        private bool ValidateTunnels(out string error)
        {
            foreach (var tunnel in tunnels)
            {
                Vector2Int mouth = tunnel.MouthCell;
                if (mouth.x < 0 || mouth.x >= width || mouth.y < 0 || mouth.y >= height)
                {
                    error = $"Tünel ({tunnel.cell.x}, {tunnel.cell.y}) ağzı grid dışında";
                    return false;
                }

                CellData mouthCell = GetCell(mouth.x, mouth.y);
                if (!mouthCell.IsWalkable)
                {
                    error = $"Tünel ({tunnel.cell.x}, {tunnel.cell.y}) ağzı geçersiz hücrede ({mouth.x}, {mouth.y})";
                    return false;
                }
            }

            error = null;
            return true;
        }

        // Çıkış satırından (y = 0) tüm yürünebilir hücrelere flood-fill; ulaşılamayan hücre hatadır.
        private bool ValidateConnectivity(out string error)
        {
            var visited = new bool[CellCount];
            var queue = new Queue<int>();

            for (int x = 0; x < width; x++)
            {
                if (cells[x].IsWalkable)
                {
                    visited[x] = true;
                    queue.Enqueue(x);
                }
            }

            if (queue.Count == 0)
            {
                error = "Çıkış satırında (y = 0) yürünebilir hücre yok";
                return false;
            }

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int cx = index % width;
                int cy = index / width;
                TryVisit(cx + 1, cy);
                TryVisit(cx - 1, cy);
                TryVisit(cx, cy + 1);
                TryVisit(cx, cy - 1);
            }

            for (int i = 0; i < CellCount; i++)
            {
                if (cells[i].IsWalkable && !visited[i])
                {
                    error = $"Hücre ({i % width}, {i / width}) çıkışa bağlı değil";
                    return false;
                }
            }

            error = null;
            return true;

            void TryVisit(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                    return;

                int i = y * width + x;
                if (visited[i] || !cells[i].IsWalkable)
                    return;

                visited[i] = true;
                queue.Enqueue(i);
            }
        }

    }
}
