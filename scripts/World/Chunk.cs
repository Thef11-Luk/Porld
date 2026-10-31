using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Godot;

namespace Porld;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct Cell
{
    public ushort Material;
    public byte State;
    public byte Flags;
}

public sealed unsafe class Chunk : IDisposable
{
    public const int Size = 256;
    public const int CellCount = Size * Size;

    public readonly int Cx, Cy;
    public Cell* Cells { get; private set; }

    public readonly Chunk?[] Neighbors = new Chunk?[9];

    public int MinX = Size, MinY = Size, MaxX = -1, MaxY = -1;
    private int _nMinX = 0, _nMinY = 0, _nMaxX = Size - 1, _nMaxY = Size - 1;

    public bool NeedsRedraw = true;
    public bool IsAwake => MinX <= MaxX;

    private readonly object _lock = new();

    public Chunk(int cx, int cy)
    {
        Cx = cx; Cy = cy;
        Cells = (Cell*)NativeMemory.AllocZeroed((nuint)CellCount, (nuint)sizeof(Cell));
        Neighbors[4] = this;
    }

    public void BeginFrame()
    {
        MinX = _nMinX; MinY = _nMinY; MaxX = _nMaxX; MaxY = _nMaxY;
        _nMinX = Size; _nMinY = Size; _nMaxX = -1; _nMaxY = -1;
    }

    private void MarkLocal(int x, int y)
    {
        if (x < _nMinX) _nMinX = x;
        if (y < _nMinY) _nMinY = y;
        if (x > _nMaxX) _nMaxX = x;
        if (y > _nMaxY) _nMaxY = y;
        NeedsRedraw = true;
    }

    public void MarkAll()
    {
        _nMinX = 0; _nMinY = 0; _nMaxX = Size - 1; _nMaxY = Size - 1;
        NeedsRedraw = true;
    }

    public void MarkDirty(int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int px = x + dx, py = y + dy;
                int ox = px < 0 ? -1 : (px >= Size ? 1 : 0);
                int oy = py < 0 ? -1 : (py >= Size ? 1 : 0);
            
                Chunk? target = Neighbors[(oy + 1) * 3 + (ox + 1)];
                if (target == null) continue;

                int lx = px - ox * Size;
                int ly = py - oy * Size;

                if (target == this) MarkLocal(lx, ly);
                else target.MarkLocalShared(lx, ly);
            }
        }
    }

    private void MarkLocalShared(int x, int y)
    {
        lock (_lock) { MarkLocal(x, y); }
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

    ~Chunk() => Dispose();
}