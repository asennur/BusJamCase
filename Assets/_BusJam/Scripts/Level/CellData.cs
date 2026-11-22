using System;

namespace BusJam.Level
{
    /// <summary>
    /// Empty varsayılan kalır ki yeni grid boş başlasın. Void: karo yok, delik (grid dışı ya da ortada boşluk).
    /// Tunnel: engel, kendi kuyruğundan yolcu çıkarır. Obstacle: yürünmez engel, tahtanın içinde karo yerine bombe olarak çizilir.
    /// Yeni değer sona eklendi ki var olan level asset'lerindeki sayılar kaymasın.
    /// </summary>
    public enum CellType : byte
    {
        Empty,
        Passenger,
        Void,
        Tunnel,
        Obstacle
    }

    public enum Cloak : byte
    {
        None,
        StartsOpen,
        StartsClosed
    }

    /// <summary>Gridde tek hücrenin kayıtlı hali.</summary>
    [Serializable]
    public struct CellData
    {
        public CellType type;
        public PassengerColor color;
        public bool hidden;
        public byte ropes;
        public Cloak cloak;

        public bool IsWalkable => type != CellType.Void && type != CellType.Tunnel && type != CellType.Obstacle;
    }
}
