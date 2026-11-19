using UnityEngine;

namespace BusJam.Save
{
    /// <summary>Oyuncu ilerlemesi (kaldÄ±ÄŸÄ± level indeksi) PlayerPrefs'e kaydedilir.</summary>
    public static class SaveSystem
    {
        public static int Coins => Mathf.Max(0, PlayerPrefs.GetInt("Coins", 0));

        public static void AddCoins(int amount)
        {
            PlayerPrefs.SetInt("Coins", (int)System.Math.Min(int.MaxValue, (long)Coins + Mathf.Max(0, amount)));
            PlayerPrefs.Save();
        }

        private const string LevelIndexKey = "LevelIndex";

        public static int GetLevelIndex()
        {
            return PlayerPrefs.GetInt(LevelIndexKey, 0);
        }

        public static void SetLevelIndex(int index)
        {
            PlayerPrefs.SetInt(LevelIndexKey, index);
            PlayerPrefs.Save();
        }
    }
}
