using Godot;
using Porld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class WorldManager : Node
{
    public static WorldManager Instance { get; private set; }
    public Dictionary<Vector2I, Chunk> Chunks = new();

    public const int ChunkWidth = 64;
    public const int ChunkHeight = 64;

    private TerrainGenerator _terrainGenerator;
    private ChunkSaver _chunkSaver;

    private Vector2I _lastPlayerChunkCoord = new Vector2I(int.MaxValue, int.MaxValue);
    private int _renderDistance = 3;

    private int _chunkToSetActive = 0;

    public event Action<Chunk> ChunkLoaded;
    public event Action<Chunk> ChunkUnloaded;

    public bool IsEvenTick = true;
    public int GlobalFrameCount = 0;

    public override void _Ready()
    {

        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;

        _terrainGenerator = new TerrainGenerator(676767);
        _chunkSaver = new ChunkSaver();
    }

    public override void _Process(double delta)
    {
        if (Chunks.Count > 0)
        {
            int indexToSetActive = _chunkToSetActive % Chunks.Keys.Count;
            Vector2I vectorToSetActive = Chunks.Keys.ToList()[indexToSetActive];
            Chunks[vectorToSetActive].InactiveTicks = 0;
            _chunkToSetActive++;
        }

        Stopwatch sp = new Stopwatch();
        sp.Start();
        bool reverseX = GlobalFrameCount % 2 == 0;

        var orderedChunks = Chunks.Values
            .OrderByDescending(c => c.ChunkCoords.Y) // Vertikal: Unten zuerst (für Schwerkraft)
            .ThenBy(c => reverseX ? -c.ChunkCoords.X : c.ChunkCoords.X); // Horizontal: Mal von links, mal von rechts!

        foreach (Chunk chunk in orderedChunks)
        {
            chunk.Simulate();
        }
        sp.Stop();
        GD.Print(sp.ElapsedMilliseconds);

        IsEvenTick = !IsEvenTick;
        GlobalFrameCount++;
    }

    public Chunk GetChunk(Vector2I chunkCoord)
    {
        Chunks.TryGetValue(chunkCoord, out var chunk);
        return chunk;
    }

    public Pixel GetPixelGlobal(int globalX, int globalY)
    {
        Vector2I chunkCoord = new Vector2I(
            globalX / ChunkWidth,
            globalY / ChunkHeight
        );

        var chunk = GetChunk(chunkCoord);
        if (chunk == null) return Pixel.OutOfBounds; // Außerhalb der Welt = Leer

        int localX = globalX % ChunkWidth;
        int localY = globalY % ChunkHeight;

        // Negative Modulo-Korrektur falls nötig, je nach Implementierung
        if (localX < 0) localX += ChunkWidth;
        if (localY < 0) localY += ChunkHeight;

        return chunk.GetPixel(localX, localY); // Angabe je nach deiner Chunk-Getter-Methode
    }

    public void SetPixelGlobal(int globalX, int globalY, Pixel pixel)
    {
        Vector2I chunkCoord = new Vector2I(
            globalX / ChunkWidth,
            globalY / ChunkHeight
        );

        var chunk = GetChunk(chunkCoord);
        if (chunk == null) return;

        int localX = globalX % ChunkWidth;
        int localY = globalY % ChunkHeight;

        if (localX < 0) localX += ChunkWidth;
        if (localY < 0) localY += ChunkHeight;

        chunk.SetPixel(localX, localY, pixel);
    }

    public void UpdateChunkStreaming(Vector2 playerGlobalPos)
    {
        int chunkPixelSize = 64;
        int playerChunkX = Mathf.FloorToInt(playerGlobalPos.X / chunkPixelSize);
        int playerChunkY = Mathf.FloorToInt(playerGlobalPos.Y / chunkPixelSize);
        Vector2I currentPlayerChunkCoord = new Vector2I(playerChunkX, playerChunkY);

        if (currentPlayerChunkCoord == _lastPlayerChunkCoord) return;
        _lastPlayerChunkCoord = currentPlayerChunkCoord;

        List<Vector2I> requiredChunks = new List<Vector2I>();

        for (int x = -_renderDistance; x <= _renderDistance; x++)
        {
            for (int y = -_renderDistance; y <= _renderDistance; y++)
            {
                Vector2I chunkCoord = new Vector2I(currentPlayerChunkCoord.X + x, currentPlayerChunkCoord.Y + y);
                requiredChunks.Add(chunkCoord);

                if (!Chunks.ContainsKey(chunkCoord))
                {
                    LoadChunk(chunkCoord);
                }
            }
        }

        List<Vector2I> chunksToUnload = new List<Vector2I>();
        foreach (var kvp in Chunks)
        {
            if (!requiredChunks.Contains(kvp.Key))
            {
                chunksToUnload.Add(kvp.Key);
            }
        }

        foreach (var coord in chunksToUnload)
        {
            UnloadChunk(coord);
        }
    }

    public void LoadChunk(Vector2I coord)
    {
        if (Chunks.ContainsKey(coord)) return;

        Chunk newChunk = new Chunk(ChunkWidth, ChunkHeight, coord);
        if (!_chunkSaver.TryLoadChunk("Dev World", newChunk))
            _terrainGenerator.GenerateChunk(newChunk);
        Chunks.Add(coord, newChunk);

        ChunkLoaded?.Invoke(newChunk);
    }

    public void UnloadChunk(Vector2I coord)
    {
        if (!Chunks.ContainsKey(coord)) return;


        Chunk removedChunk = Chunks[coord];
        _chunkSaver.SaveChunk("Dev World", removedChunk);

        Chunks.Remove(coord);

        ChunkUnloaded?.Invoke(removedChunk);
    }
}
