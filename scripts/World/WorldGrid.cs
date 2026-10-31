using System;
using Godot;
using Porld;

public class WorldGrid
{
    public int ChunksX { get; }
    public int ChunksY { get; }
    private UnsafeChunk[,] _chunks;
    private bool _isEvenTick = false;

    public WorldGrid(int chunksX, int chunksY)
    {
        ChunksX = chunksX;
        ChunksY = chunksY;
        _chunks = new UnsafeChunk[chunksX, chunksY];

        for (int cx = 0; cx < ChunksX; cx++)
        {
            for (int cy = 0; cy < ChunksY; cy++)
            {
                _chunks[cx, cy] = new UnsafeChunk();
            }
        }
    }

    public void SetPixel(int worldX, int worldY, byte value)
    {
        int cx = worldX / UnsafeChunk.InnerSize;
        int cy = worldY / UnsafeChunk.InnerSize;
        if (cx >= 0 && cx < ChunksX && cy >= 0 && cy < ChunksY)
        {
            int lx = worldX % UnsafeChunk.InnerSize;
            int ly = worldY % UnsafeChunk.InnerSize;
            _chunks[cx, cy].SetCellLocal(lx, ly, value);
        }
    }

    public void SimulateStep()
    {
        _isEvenTick = !_isEvenTick;

        ExchangeGhostBorders();

        for (int cy = ChunksY - 1; cy >= 0; cy--)
        {
            for (int cx = 0; cx < ChunksX; cx++)
            {
                var chunk = _chunks[cx, cy];
                chunk.PrepareStep();
                chunk.Simulate(_isEvenTick);
            }
        }
    }
     
    public void ExchangeGhostBorders()
    {
        for (int cx = 0; cx < ChunksX; cx++)
        {
            for (int cy = 0; cy < ChunksY; cy++)
            {
                var current = _chunks[cx, cy];

                if (cy > 0)
                {
                    var top = _chunks[cx, cy - 1];
                    for (int x = 0; x < UnsafeChunk.InnerSize; x++);
                }
            }
        }
    }
}