using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Porld;
using System.Linq;

public sealed class World
{
    private readonly Dictionary<Vector2I, Chunk> _chunks = new();

    public IReadOnlyDictionary<Vector2I, Chunk> Chunks => _chunks;

    private struct Row
    {
        public Chunk[] Even;
        public Chunk[] Odd;
    }

    private Row[] _rows = System.Array.Empty<Row>();
    private bool _scheduleDirty = true;

    public void StepRowWise(int frame)
    {
        foreach (var chunk in _chunks.Values)
            chunk.BeginFrame();

        bool ltr = (frame & 1) == 0;

        var bands = _chunks.Values.GroupBy(c => c.Cy).OrderByDescending(g => g.Key);
        foreach (var band in bands)
        {
            var list = ltr ? band.OrderBy(c => c.Cx).ToArray()
                           : band.OrderByDescending(c => c.Cx).ToArray();

            for (int y = Chunk.Size - 1; y >= 0; y--)
                foreach (var chunk in list)
                    if (chunk.IsAwake) Simulator.StepRow(chunk, y, frame, ltr);
        }
    }

    public Chunk? GetChunk(Vector2I pos)
    {
        _chunks.TryGetValue(pos, out var chunk);
        return chunk;
    }

    public Chunk LoadChunk(Vector2I pos)
    {
        if (_chunks.TryGetValue(pos, out var existing))
            return existing;

        var chunk = new Chunk(pos.X, pos.Y);
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
                    neighbor.MarkAll();
                }
            }
        }

        _scheduleDirty = true;
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

    private void RebuildSchedule()
    {
        _rows = _chunks.Values
            .GroupBy(c => c.Cy)
            .OrderByDescending(g => g.Key)                       // unterste Reihe zuerst
            .Select(g => new Row
            {
                Even = g.Where(c => (c.Cx & 1) == 0).ToArray(),
                Odd = g.Where(c => (c.Cx & 1) != 0).ToArray(),
            })
            .ToArray();
        _scheduleDirty = false;
    }

    public void Step(int frame)
    {
        if (_scheduleDirty) RebuildSchedule();

        foreach (var chunk in _chunks.Values)
            chunk.BeginFrame();                                   // erst alle umschalten

        bool evenFirst = (frame & 1) == 0;
        foreach (var row in _rows)
        {
            RunPhase(evenFirst ? row.Even : row.Odd, frame);
            RunPhase(evenFirst ? row.Odd : row.Even, frame);
        }
    }

    private static void RunPhase(Chunk[] chunks, int frame)
    {
        int awake = 0;
        for (int i = 0; i < chunks.Length; i++)
            if (chunks[i].IsAwake) awake++;

        if (awake == 0) return;

        if (awake == 1)                                            // Threads lohnen sich nicht
        {
            for (int i = 0; i < chunks.Length; i++)
                if (chunks[i].IsAwake) Simulator.StepChunk(chunks[i], frame);
            return;
        }

        System.Threading.Tasks.Parallel.For(0, chunks.Length, i =>
        {
            if (chunks[i].IsAwake) Simulator.StepChunk(chunks[i], frame);
        });
    }
}
