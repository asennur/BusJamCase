namespace BusJam.Core
{
    /// <summary>Oyunun değişmeyen sabitleri.</summary>
    public static class GameRules
    {
        public const int BusCapacity = 3;

        // Kamera sabit; grid + üstündeki yol/slot alanı bu sınırın içine elle kuruldu (Z5.1, Asen kararı).
        public const int MaxWidth = 7;
        public const int MaxHeight = 9;

        // Slot sayısı sabit 7, level verisinden gelmez (Asen kuralı). Uçtaki iki slot level numarasına göre açılır.
        public const int VisibleSlotCount = 7;
        public const int FirstSlotUnlockLevel = 50;
        public const int LastSlotUnlockLevel = 100;

        /// <summary>Uçtaki iki slot (index 0 ve VisibleSlotCount - 1) level numarasına göre kilitli başlar, ortadakiler hep açık.</summary>
        public static bool IsSlotUnlocked(int slotIndex, int levelNumber)
        {
            if (slotIndex == 0)
                return levelNumber >= FirstSlotUnlockLevel;
            if (slotIndex == VisibleSlotCount - 1)
                return levelNumber >= LastSlotUnlockLevel;
            return true;
        }
    }
}
