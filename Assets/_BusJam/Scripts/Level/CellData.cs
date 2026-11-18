using System;

namespace BusJam.Level
{
    /// <summary>Empty varsayılan kalır ki yeni grid boş başlasın. Void: grid dışı, yürünmez, çizilmez.</summary>
    public enum CellType : byte
    {
        Empty,
        Passenger,
        Void
    }

    /// <summary>Gridde tek hücrenin kayıtlı hali.</summary>
    [Serializable]
    public struct CellData
    {
        public CellType type;
        public PassengerColor color;
        public bool hidden;

        public bool IsWalkable => type != CellType.Void;
    }
}
