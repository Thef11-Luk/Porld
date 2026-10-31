using Godot;
using Porld;

public static unsafe class Simulator
{
    private const int FlowRange = 4;
    public static uint Hash(int x, int y, int frame)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)frame * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    public static void StepChunk(Chunk chunk, int frame)
    {
        if (!chunk.IsAwake) return;                       // schlafender Chunk kostet nichts

        byte stamp = (byte)frame;
        bool leftToRight = (frame & 1) == 0;
        int width = chunk.MaxX - chunk.MinX + 1;

        for (int y = chunk.MaxY; y >= chunk.MinY; y--)
        {
            for (int i = 0; i < width; i++)
            {
                int x = leftToRight ? chunk.MinX + i : chunk.MaxX - i;
                Cell* cell = chunk.Cells + y * Chunk.Size + x;

                if (cell->Material == Materials.Empty) continue;

                if (cell->Flags == stamp)                 // schon bewegt (oder seltener Zufallstreffer)
                {
                    chunk.MarkDirty(x, y);
                    continue;
                }

                MaterialType type = Materials.Types[cell->Material];
                if (type == MaterialType.Powder)
                    UpdatePowder(chunk, x, y, cell, stamp, frame);
                else if (type == MaterialType.Liquid)
                    UpdateLiquid(chunk, x, y, cell, stamp, frame);
            }
        }
    }

    private static bool UpdatePowder(Chunk chunk, int x, int y, Cell* cell, byte stamp, int frame)
    {
        if (TryMove(chunk, x, y, x, y + 1, cell, stamp)) return true;

        int wx = chunk.Cx * Chunk.Size + x;
        int wy = chunk.Cy * Chunk.Size + y;
        int dir = (Hash(wx, wy, frame) & 1) == 0 ? -1 : 1;

        if (TryMove(chunk, x, y, x + dir, y + 1, cell, stamp)) return true;
        if (TryMove(chunk, x, y, x - dir, y + 1, cell, stamp)) return true;
        return false;
    }

    private static bool UpdateLiquid(Chunk chunk, int x, int y, Cell* cell, byte stamp, int frame)
    {
        // 1. nach unten
        if (TryMove(chunk, x, y, x, y + 1, cell, stamp)) return true;

        int wx = chunk.Cx * Chunk.Size + x;
        int wy = chunk.Cy * Chunk.Size + y;
        uint h = Hash(wx, wy, frame);

        // 2. diagonal nach unten
        int dir = (h & 1) == 0 ? -1 : 1;
        if (TryMove(chunk, x, y, x + dir, y + 1, cell, stamp)) return true;
        if (TryMove(chunk, x, y, x - dir, y + 1, cell, stamp)) return true;

        // 3. seitlich fließen (anderes Hash-Bit als oben)
        int side = ((h >> 1) & 1) == 0 ? -1 : 1;
        if (TryFlow(chunk, x, y, side, cell, stamp)) return true;
        if (TryFlow(chunk, x, y, -side, cell, stamp)) return true;

        return false;
    }

    private static bool TryMove(Chunk chunk, int x, int y, int tx, int ty, Cell* cell, byte stamp)
    {
        Cell* target = chunk.GetCell(tx, ty);
        if (target == null) return false;

        if (!CanDisplace(cell->Material, target->Material)) return false;
        if (target->Material != Materials.Empty && target->Flags == stamp) return false;

        Cell tmp = *cell;
        *cell = *target;
        *target = tmp;

        cell->Flags = stamp;
        target->Flags = stamp;

        chunk.MarkDirty(x, y);
        chunk.MarkDirty(tx, ty);
        return true;
    }

    private static bool TryFlow(Chunk chunk, int x, int y, int dir, Cell* cell, byte stamp)
    {
        int furthest = 0;

        for (int i = 1; i <= FlowRange; i++)
        {
            Cell* target = chunk.GetCell(x + dir * i, y);
            if (target == null) break;                              // ungeladen = Wand
            if (!CanDisplace(cell->Material, target->Material)) break;

            if (target->Material != Materials.Empty && target->Flags == stamp)
            {
                chunk.MarkDirty(x, y);                              // blockiert nur durch Timing: nächsten Frame nochmal versuchen
                break;
            }
            furthest = i;
        }

        if (furthest == 0) return false;
        return TryMove(chunk, x, y, x + dir * furthest, y, cell, stamp);
    }

    private static bool CanDisplace(ushort mover, ushort target)
    {
        if (target == Materials.Empty) return true;

        MaterialType t = Materials.Types[target];
        if (t != MaterialType.Liquid && t != MaterialType.Gas) return false;

        return Materials.Densities[mover] > Materials.Densities[target];
    }
}