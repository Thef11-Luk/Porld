using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Porld;

public sealed unsafe class Chunk : IDisposable
{
    public const int Size = 64;
    public const int CellCount = Size * Size;

    public readonly int Cx, Cy;
    public Cell* Cells { get; private set; }

    // 3x3-Nachbarn, Index = (dy+1)*3 + (dx+1), Mitte (4) = this. null = nicht geladen = Wand.
    public readonly Chunk?[] Neighbors = new Chunk?[9];

    // Dirty Rect (Grenzen inklusive): Bereich, der in DIESEM Frame simuliert wird.
    public int MinX = Size, MinY = Size, MaxX = -1, MaxY = -1;
    // Dirty Rect für den NÄCHSTEN Frame. Neuer Chunk: alles dirty.
    private int _nMinX = 0, _nMinY = 0, _nMaxX = Size - 1, _nMaxY = Size - 1;

    public bool NeedsRedraw = true;
    public bool IsAwake => MinX <= MaxX;

    public bool Ready;   // alle 8 Nachbarn geladen -> darf simuliert werden

    public bool Modified;   // seit Laden verändert -> muss beim Entladen gespeichert werden


    public Chunk(int cx, int cy)
    {
        Cx = cx; Cy = cy;
        Cells = (Cell*)NativeMemory.AllocZeroed((nuint)CellCount, (nuint)sizeof(Cell));
        Neighbors[4] = this;
    }

    // Markiert den Chunk, in dem die (evtl. über den Rand hinausragende) Zelle liegt.
    public void MarkModified(int x, int y)
    {
        int dx = x < 0 ? -1 : (x >= Size ? 1 : 0);
        int dy = y < 0 ? -1 : (y >= Size ? 1 : 0);
        var c = Neighbors[(dy + 1) * 3 + (dx + 1)];
        if (c != null) c.Modified = true;
    }
    public bool HasAllNeighbors()
    {
        for (int i = 0; i < 9; i++)
            if (Neighbors[i] == null) return false;
        return true;
    }

    // Für ALLE Chunks aufrufen, bevor der erste simuliert wird.
    public void BeginFrame()
    {
        MinX = _nMinX; MinY = _nMinY; MaxX = _nMaxX; MaxY = _nMaxY;
        _nMinX = Size; _nMinY = Size; _nMaxX = -1; _nMaxY = -1;
    }

    public void SetMaterial(int x, int y, ushort material)
    {
        Cells[y * Size + x].Material = material;
        MarkDirty(x, y);
    }

    public void MarkAll()
    {
        _nMinX = 0; _nMinY = 0; _nMaxX = Size - 1; _nMaxY = Size - 1;
        NeedsRedraw = true;
    }

    private void MarkLocal(int x, int y)
    {
        if (x < _nMinX) _nMinX = x;
        if (y < _nMinY) _nMinY = y;
        if (x > _nMaxX) _nMaxX = x;
        if (y > _nMaxY) _nMaxY = y;
        NeedsRedraw = true;
    }

    // Markiert (x,y) und die 8 Nachbarn für den nächsten Frame.
    // Koordinaten über den Rand hinaus wecken den Nachbarchunk.
    public void MarkDirty(int x, int y)
    {
        // Schnellweg: Das Rect ist eine Bounding Box, zwei Ecken des 3x3-Bereichs genügen.
        if (x > 0 && x < Size - 1 && y > 0 && y < Size - 1)
        {
            MarkLocal(x - 1, y - 1);
            MarkLocal(x + 1, y + 1);
            return;
        }

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int px = x + dx, py = y + dy;
                int ox = px < 0 ? -1 : (px >= Size ? 1 : 0);
                int oy = py < 0 ? -1 : (py >= Size ? 1 : 0);

                Neighbors[(oy + 1) * 3 + (ox + 1)]?.MarkLocal(px - ox * Size, py - oy * Size);
            }
        }
    }

    // Lokale Koordinaten, dürfen bis zu einen Chunk außerhalb liegen. null = Wand.
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