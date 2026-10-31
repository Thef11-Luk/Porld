using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Porld;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct Cell
{
    public ushort Material;
    public byte State;
    public byte Flags;
}

public sealed unsafe class UnsafeChunk : IDisposable
{
    public const int Size = 64;
    public const int CellCount = Size * Size;

    public readonly int Cx, Cy;
    public Cell* Cells { get; private set; }

    public readonly UnsafeChunk?[] Neighbors = new UnsafeChunk?[9];

    public UnsafeChunk(int cx, int cy)
    {
        Cx = cx; Cy = cy;
        Cells = (Cell*)NativeMemory.AllocZeroed((nuint)CellCount, (nuint)sizeof(Cell));
        Neighbors[4] = this;
    }

    public Cell* GetCell(int x, int y)
    {
        int dx = x < 0 ? -1 : (x >= Size ? 1 : 0);
        int dy = y < 0 ? -1 : (y >= Size ? 1 : 0);
        var chunk = Neighbors[(dy + 1) * 3 + (dx + 1)];
        if (chunk == null) return null;

        int lx = x - dx * Size;
        int ly = y - dy * Size;
        Debug.Assert(lx >= 0 && lx < Size && ly >= 0 && ly < Size);
        return chunk.Cells + ly * Size + lx;
    }

    public void Dispose()
    {
        if (Cells != null)
        {
            NativeMemory.Free(Cells);
            Cells = null;
        }
        GC.SuppressFinalize(this);
    }

    ~UnsafeChunk() => Dispose();
}