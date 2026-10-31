using System.Runtime.InteropServices;

namespace Porld;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct Cell
{
    public ushort Material;   // 0 = leer
    public byte State;        // frei für Lebenszeit, Temperatur usw.
    public byte Flags;        // Stempel: (byte)frame der letzten Bewegung
}