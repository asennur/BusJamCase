namespace BusJam.Core
{
    /// <summary>Oyunun değişmeyen sabitleri.</summary>
    public static class GameRules
    {
        public const int BusCapacity = 3;

        // Kamera sabit; grid + üstündeki yol/slot alanı bu sınırın içine elle kuruldu (Z5.1, Asen kararı).
        public const int MaxWidth = 7;
        public const int MaxHeight = 9;
    }
}
