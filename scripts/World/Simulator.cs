using Porld;

public static unsafe class Simulator
{
    private const int FlowRange = 4;   // max. Zellen, die Flüssigkeit pro Frame seitlich springt

    // Reproduzierbarer Zufall aus Position und Frame, unabhängig von der Update-Reihenfolge.
    public static uint Hash(int x, int y, int frame)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)frame * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    // Simuliert eine Zeile eines Chunks innerhalb seines Dirty Rects.
    public static void StepRow(Chunk chunk, int y, int frame, bool leftToRight)
    {
        if (y < chunk.MinY || y > chunk.MaxY) return;

        // Stempel = Frame-Nummer. Eine Zelle mit aktuellem Stempel wurde in diesem Frame
        // schon bewegt. Ruhende Zellen müssen nie neu gestempelt werden.
        byte stamp = (byte)frame;
        int width = chunk.MaxX - chunk.MinX + 1;

        for (int i = 0; i < width; i++)
        {
            int x = leftToRight ? chunk.MinX + i : chunk.MaxX - i;
            Cell* cell = chunk.Cells + y * Chunk.Size + x;

            if (cell->Material == Materials.Empty) continue;

            if (cell->Flags == stamp)
            {
                chunk.MarkDirty(x, y);   // im nächsten Frame nochmal ansehen
                continue;
            }

            MaterialType type = Materials.Types[cell->Material];
            if (type == MaterialType.Powder)
                UpdatePowder(chunk, x, y, cell, stamp, frame);
            else if (type == MaterialType.Liquid)
                UpdateLiquid(chunk, x, y, cell, stamp, frame);
        }
    }

    private static void UpdatePowder(Chunk chunk, int x, int y, Cell* cell, byte stamp, int frame)
    {
        if (TryMove(chunk, x, y, x, y + 1, cell, stamp)) return;

        int dir = RandomDir(chunk, x, y, frame, 0);
        if (TryMove(chunk, x, y, x + dir, y + 1, cell, stamp)) return;
        TryMove(chunk, x, y, x - dir, y + 1, cell, stamp);
    }

    private static void UpdateLiquid(Chunk chunk, int x, int y, Cell* cell, byte stamp, int frame)
    {
        // Schneller Ausstieg: unten, diagonal und seitlich alles blockiert, nichts zu tun.
        if (IsBlocked(chunk, x, y + 1, cell) && IsBlocked(chunk, x - 1, y + 1, cell) &&
            IsBlocked(chunk, x + 1, y + 1, cell) && IsBlocked(chunk, x - 1, y, cell) &&
            IsBlocked(chunk, x + 1, y, cell))
            return;

        // 1. fallen
        if (TryMove(chunk, x, y, x, y + 1, cell, stamp)) return;

        // 2. diagonal rutschen (Hash-Bit 0)
        int dir = RandomDir(chunk, x, y, frame, 0);
        if (TryMove(chunk, x, y, x + dir, y + 1, cell, stamp)) return;
        if (TryMove(chunk, x, y, x - dir, y + 1, cell, stamp)) return;

        // 3. seitlich fließen (Hash-Bit 1, unabhängig von Bit 0)
        int side = RandomDir(chunk, x, y, frame, 1);
        if (TryFlow(chunk, x, y, side, cell, stamp)) return;
        TryFlow(chunk, x, y, -side, cell, stamp);
    }

    // -1 oder +1, abhängig von Weltposition, Frame und Bit-Nummer.
    private static int RandomDir(Chunk chunk, int x, int y, int frame, int bit)
    {
        int wx = chunk.Cx * Chunk.Size + x;
        int wy = chunk.Cy * Chunk.Size + y;
        return ((Hash(wx, wy, frame) >> bit) & 1) == 0 ? -1 : 1;
    }

    // Tauscht die Zelle mit dem Ziel, falls erlaubt.
    private static bool TryMove(Chunk chunk, int x, int y, int tx, int ty, Cell* cell, byte stamp)
    {
        Cell* target = chunk.GetCell(tx, ty);
        if (target == null) return false;                                   // Wand

        if (!CanDisplace(cell->Material, target->Material)) return false;
        if (target->Material != Materials.Empty && target->Flags == stamp) return false;

        Cell tmp = *cell;
        *cell = *target;
        *target = tmp;

        cell->Flags = stamp;
        target->Flags = stamp;

        chunk.Modified = true;
        chunk.MarkModified(tx, ty);   // Ziel kann im Nachbarchunk liegen

        chunk.MarkDirty(x, y);
        chunk.MarkDirty(tx, ty);
        return true;
    }

    // Springt bis zu FlowRange Zellen seitlich, direkt zur weitesten freien Zelle.
    private static bool TryFlow(Chunk chunk, int x, int y, int dir, Cell* cell, byte stamp)
    {
        int furthest = 0;

        for (int i = 1; i <= FlowRange; i++)
        {
            Cell* target = chunk.GetCell(x + dir * i, y);
            if (target == null) break;
            if (!CanDisplace(cell->Material, target->Material)) break;

            if (target->Material != Materials.Empty && target->Flags == stamp)
            {
                chunk.MarkDirty(x, y);   // nur durch Timing blockiert: nächsten Frame wieder versuchen
                break;
            }
            furthest = i;
        }

        return furthest != 0 && TryMove(chunk, x, y, x + dir * furthest, y, cell, stamp);
    }

    // Leer ist frei. Flüssigkeit/Gas nur, wenn der Mover dichter ist. Feststoffe blockieren.
    private static bool CanDisplace(ushort mover, ushort target)
    {
        if (target == Materials.Empty) return true;

        MaterialType t = Materials.Types[target];
        if (t != MaterialType.Liquid && t != MaterialType.Gas) return false;

        return Materials.Densities[mover] > Materials.Densities[target];
    }

    private static bool IsBlocked(Chunk chunk, int x, int y, Cell* cell)
    {
        Cell* t = chunk.GetCell(x, y);
        return t == null || !CanDisplace(cell->Material, t->Material);
    }
}