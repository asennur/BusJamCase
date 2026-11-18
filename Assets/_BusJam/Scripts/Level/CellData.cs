using System;

namespace BusJam.Level
{
    /// <summary>Empty varsayılan kalır ki yeni grid boş başlasın. Void: grid dışı, yürünmez, çizilmez. Tunnel: engel, kendi kuyruğundan yolcu çıkarır.</summary>
    public enum CellType : byte
    {
        Empty,
        Passenger,
        Void,
        Tunnel
    }

    /// <summary>Gridde tek hücrenin kayıtlı hali.</summary>
    [Serializable]
    public struct CellData
    {
        public CellType type;
        public PassengerColor color;
        public bool hidden;

        public bool IsWalkable => type != CellType.Void && type != CellType.Tunnel;
    }
}
