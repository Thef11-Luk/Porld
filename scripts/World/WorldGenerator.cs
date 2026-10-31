using System.Threading;
using Godot;
using Porld;

public interface IWorldGenerator
{
    // Füllt den Chunk (Cells sind beim Aufruf alle 0). Muss aus (cx, cy, seed) deterministisch sein
    // und darf nur auf den übergebenen Chunk schreiben, weil es später in einem Hintergrund-Thread läuft.
    void Generate(Chunk chunk);
}

public sealed unsafe class FlatGenerator : IWorldGenerator
{
    private const int GroundY = 256;

    public void Generate(Chunk chunk)
    {
        int worldTop = chunk.Cy * Chunk.Size;
        if (worldTop + Chunk.Size <= GroundY) return;

        for (int y = 0; y < Chunk.Size; y++)
        {
            if (worldTop + y < GroundY) continue;
            for (int x = 0; x < Chunk.Size; x++)
            {
                chunk.Cells[y * Chunk.Size + x].Material = Materials.Stone;
            }
        }
    }
}