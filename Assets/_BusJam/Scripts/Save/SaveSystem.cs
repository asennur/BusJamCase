using UnityEngine;

namespace BusJam.Save
{
    /// <summary>Persists level progress, tutorials and coins in PlayerPrefs.</summary>
    public static class SaveSystem
    {
        public static event System.Action ProgressReset;
        public static event System.Action<int> CoinsChanged;
        public static int Coins => Mathf.Max(0, PlayerPrefs.GetInt("Coins", 0));

        public static void AddCoins(int amount)
        {
            int before = Coins;
            int after = (int)System.Math.Min(int.MaxValue, (long)before + Mathf.Max(0, amount));
            if (before == after) return;
            PlayerPrefs.SetInt("Coins", after);
            PlayerPrefs.Save();
            CoinsChanged?.Invoke(after);
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
            CoinsChanged?.Invoke(0);
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
