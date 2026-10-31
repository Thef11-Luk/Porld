using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Porld;

public sealed class World
{
    private readonly Dictionary<Vector2I, Chunk> _chunks = new();
    public IReadOnlyDictionary<Vector2I, Chunk> Chunks => _chunks;

    // Eine Chunk-Reihe, einmal nach Cx aufsteigend und einmal absteigend sortiert.
    private struct Row
    {
        public Chunk[] Asc;
        public Chunk[] Desc;
    }

    private Row[] _rows = Array.Empty<Row>();
    private bool _scheduleDirty = true;

    public Chunk? GetChunk(Vector2I pos)
    {
        _chunks.TryGetValue(pos, out var chunk);
        return chunk;
    }

    // Laden/Entladen nur zwischen zwei Simulationsschritten aufrufen!
    public Chunk LoadChunk(Vector2I pos)
    {
        if (_chunks.TryGetValue(pos, out var existing))
            return existing;

        var chunk = new Chunk(pos.X, pos.Y);
        Insert(chunk);
        return chunk;
    }

    // Hängt einen fertigen Chunk ein. Nur im Hauptthread, nie während Step.
    public bool Insert(Chunk chunk)
    {
        var pos = new Vector2I(chunk.Cx, chunk.Cy);
        if (_chunks.ContainsKey(pos)) return false;

        _chunks[pos] = chunk;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                if (!_chunks.TryGetValue(pos + new Vector2I(dx, dy), out var neighbor)) continue;

                chunk.Neighbors[(dy + 1) * 3 + (dx + 1)] = neighbor;
                neighbor.Neighbors[(-dy + 1) * 3 + (-dx + 1)] = chunk;
                neighbor.MarkAll();
            }
        }

        _scheduleDirty = true;
        return true;
    }

    public void UnloadChunk(Vector2I pos)
    {
        if (!_chunks.Remove(pos, out var chunk))
            return;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var neighbor = chunk.Neighbors[(dy + 1) * 3 + (dx + 1)];
                if (neighbor == null) continue;

                neighbor.Neighbors[(-dy + 1) * 3 + (-dx + 1)] = null;
                neighbor.MarkAll();
            }
        }

        _scheduleDirty = true;
        chunk.Dispose();
    }

    private void RebuildSchedule()
    {
        foreach (var c in _chunks.Values)
            c.Ready = c.HasAllNeighbors();

        _rows = _chunks.Values
            .Where(c => c.Ready)
            .GroupBy(c => c.Cy)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var asc = g.OrderBy(c => c.Cx).ToArray();
                var desc = (Chunk[])asc.Clone();
                Array.Reverse(desc);
                return new Row { Asc = asc, Desc = desc };
            })
            .ToArray();
        _scheduleDirty = false;
    }

    // Reihenfolge: Chunk-Reihen von unten nach oben. Innerhalb einer Reihe wird ZEILENWEISE
    // über alle Chunks gearbeitet (nicht Chunk für Chunk), damit sich Chunk-Grenzen wie
    // Zellen im Chunk-Inneren verhalten. Das hat die Stufen im Wasser größtenteils beseitigt.
    public void Step(int frame)
    {
        if (_scheduleDirty) RebuildSchedule();

        foreach (var row in _rows)
            foreach (var chunk in row.Asc)
                chunk.BeginFrame();

        bool ltr = (frame & 1) == 0;

        foreach (var row in _rows)
        {
            var chunks = ltr ? row.Asc : row.Desc;
            for (int y = Chunk.Size - 1; y >= 0; y--)
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    if (chunks[i].IsAwake)
                        Simulator.StepRow(chunks[i], y, frame, ltr);
                }
            }
        }
    }
}