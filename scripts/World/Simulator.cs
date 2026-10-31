using Godot;
using Porld;

public static unsafe class Simulator
{
    private const int FlowRange = 4;

    public static int Visited, Moved;
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
        if (!chunk.IsAwake) return;
        bool ltr = (frame & 1) == 0;
        for (int y = chunk.MaxY; y >= chunk.MinY; y--)
            StepRow(chunk, y, frame, ltr);
    }

    public static void StepRow(Chunk chunk, int y, int frame, bool leftToRight)
    {
        if (y < chunk.MinY || y > chunk.MaxY) return;

        byte stamp = (byte)frame;
        int width = chunk.MaxX - chunk.MinX + 1;

        for (int i = 0; i < width; i++)
        {
            int x = leftToRight ? chunk.MinX + i : chunk.MaxX - i;
            Cell* cell = chunk.Cells + y * Chunk.Size + x;

            if (cell->Material == Materials.Empty) continue;
            Visited++;

            if (cell->Flags == stamp)
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
        if (IsBlocked(chunk, x, y + 1, cell) && IsBlocked(chunk, x - 1, y + 1, cell) &&
    IsBlocked(chunk, x + 1, y + 1, cell) && IsBlocked(chunk, x - 1, y, cell) &&
    IsBlocked(chunk, x + 1, y, cell))
            return false;
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

        if (HasFreeRowAhead(chunk, x, y, cell))
            chunk.MarkDirty(x, y);

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
        Moved++;
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

    // Liegt in einer Richtung hinter gleichem Material noch freier Platz?
    private static bool HasFreeRowAhead(Chunk chunk, int x, int y, Cell* cell)
    {
        for (int dir = -1; dir <= 1; dir += 2)
        {
            for (int i = 1; i <= FlowRange; i++)
            {
                Cell* t = chunk.GetCell(x + dir * i, y);
                if (t == null) break;
                if (t->Material == Materials.Empty) return true;
                if (t->Material != cell->Material) break;
            }
        }
        return false;
    }

    private static bool IsBlocked(Chunk chunk, int x, int y, Cell* cell)
    {
        Cell* t = chunk.GetCell(x, y);
        return t == null || !CanDisplace(cell->Material, t->Material);
    }
}