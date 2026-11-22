namespace BusJam.Core
{
    /// <summary>Oyunun değişmeyen sabitleri.</summary>
    public static class GameRules
    {
        public const int BusCapacity = 3;

        // İpli yolcu en çok 3 kat ip taşır; her kat gövdede ayrı bir halka (Asen, 22 Kasım).
        public const int MaxRopes = 3;

        // Grid sınırı 9×9 (Asen, 22 Kasım); referanstan (7×8) büyük gridlerde CameraFitter kamerayı geri çeker.
        public const int MaxWidth = 9;
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
