using UnityEngine;

namespace BusJam.Save
{
    /// <summary>Oyuncu ilerlemesi (kaldÄ±ÄŸÄ± level indeksi) PlayerPrefs'e kaydedilir.</summary>
    public static class SaveSystem
    {
        public static event System.Action ProgressReset;
        public static int Coins => Mathf.Max(0, PlayerPrefs.GetInt("Coins", 0));

        public static void AddCoins(int amount)
        {
            PlayerPrefs.SetInt("Coins", (int)System.Math.Min(int.MaxValue, (long)Coins + Mathf.Max(0, amount)));
            PlayerPrefs.Save();
        }

        private const string LevelIndexKey = "LevelIndex";

        public static bool TutorialSeen(int level) => PlayerPrefs.GetInt(TutorialKey(level), 0) != 0;

        public static void CompleteTutorial(int level)
        {
            PlayerPrefs.SetInt(TutorialKey(level), 1);
            PlayerPrefs.Save();
        }

        private static string TutorialKey(int level) => "TutorialSeen_" + level;

        public static void ResetProgress()
        {
            PlayerPrefs.SetInt(LevelIndexKey, 0);
            PlayerPrefs.SetInt("Coins", 0);
            PlayerPrefs.DeleteKey(TutorialKey(1));
            PlayerPrefs.DeleteKey(TutorialKey(6));
            PlayerPrefs.DeleteKey(TutorialKey(8));
            PlayerPrefs.Save();
            ProgressReset?.Invoke();
        }

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
