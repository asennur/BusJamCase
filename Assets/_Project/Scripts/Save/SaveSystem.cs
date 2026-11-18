using UnityEngine;

namespace BusJam.Save
{
    /// <summary>Oyuncu ilerlemesi (kaldığı level indeksi) PlayerPrefs'e kaydedilir.</summary>
    public static class SaveSystem
    {
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
