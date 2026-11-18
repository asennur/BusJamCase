using System.Collections.Generic;
using UnityEngine;

namespace BusJam.Level
{
    /// <summary>Levellerin oynanış sırası. Liste bitince baştan döner, level numarası artmaya devam eder.</summary>
    [CreateAssetMenu(menuName = "BusJam/Level Database", fileName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField] private List<LevelData> levels = new List<LevelData>();

        public int Count => levels.Count;

        public LevelData Get(int levelIndex) => levels[levelIndex % levels.Count];

#if UNITY_EDITOR
        public void Add(LevelData level)
        {
            if (!levels.Contains(level))
                levels.Add(level);
        }
#endif
    }
}
