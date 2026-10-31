using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Porld;

public sealed class World
{
    private readonly Dictionary<Vector2I, UnsafeChunk> _chunks = new();

    public IReadOnlyDictionary<Vector2I, UnsafeChunk> Chunks => _chunks;

    public UnsafeChunk? GetChunk(Vector2I pos)
    {
        _chunks.TryGetValue(pos, out var chunk);
        return chunk;
    }

    public UnsafeChunk LoadChunk(Vector2I pos)
    {
        if (_chunks.TryGetValue(pos, out var existing))
            return existing;

        var chunk = new UnsafeChunk(pos.X, pos.Y);
        _chunks[pos] = chunk;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;

                if (_chunks.TryGetValue(pos + new Vector2I(dx, dy), out var neighbor))
                {
                    chunk.Neighbors[(dy + 1) * 3 + (dx + 1)] = neighbor;
                    neighbor.Neighbors[(-dy + 1) * 3 + (-dx + 1)] = chunk;
                }
            }
        }

        return chunk;
    }

    public void UnloadChunk(Vector2I pos)
    {
        if (!_chunks.Remove(pos, out var chunk))
            return;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                var neighbor = chunk.Neighbors[(dy + 1) * 3 + (dx + 1)];
                if (neighbor != null)
                {
                    neighbor.Neighbors[(-dy + 1) * 3 + (-dx + 1)] = null;
                }
            }
        }

        chunk.Dispose();
    }
}
