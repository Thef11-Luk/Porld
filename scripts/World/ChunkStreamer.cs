using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Porld;

public sealed class ChunkStreamer
{
    public int MaxInFlight = 8;                 // gleichzeitig laufende Generierungsaufträge
    public int MaxInsertsPerFrame = 4;          // fertige Chunks, die pro Frame eingehängt werden
    public int MaxUnloadsPerFrame = 4;
    public int MarginX = 2, MarginY = 2;        // Rand um den sichtbaren Bereich (Laden)
    public int HysteresisX = 1, HysteresisY = 1;

    public Action<Chunk>? ChunkLoaded;
    public Action<Chunk>? ChunkUnloading;       // VOR dem Entladen

    private readonly World _world;
    private readonly IWorldGenerator _generator;

    private readonly ConcurrentQueue<Chunk> _done = new();
    private readonly HashSet<Vector2I> _inFlight = new();   // nur Hauptthread
    private readonly List<Vector2I> _toLoad = new();
    private readonly List<Vector2I> _toUnload = new();

    private Vector2I _center = new(int.MinValue, int.MinValue);
    private int _rx, _ry;
    private bool _rescan = true;

    private readonly ChunkStore _store;

    public ChunkStreamer(World world, IWorldGenerator generator, ChunkStore store)
    {
        _world = world;
        _generator = generator;
        _store = store;
    }

    // Zwischen zwei Simulationsschritten aufrufen (z. B. in _Process).
    public void Update(Vector2 camCenter, Vector2 halfView)
    {
        var center = new Vector2I(
            Mathf.FloorToInt(camCenter.X / Chunk.Size),
            Mathf.FloorToInt(camCenter.Y / Chunk.Size));
        int rx = Mathf.CeilToInt(halfView.X / Chunk.Size) + MarginX;
        int ry = Mathf.CeilToInt(halfView.Y / Chunk.Size) + MarginY;

        if (center != _center || rx != _rx || ry != _ry)
        {
            _center = center; _rx = rx; _ry = ry;
            _rescan = true;
        }

        DrainFinished();                         // immer, ist billig wenn die Queue leer ist

        if (!_rescan) return;
        _rescan = false;

        Unload();
        StartJobs();
    }

    private void DrainFinished()
    {
        for (int i = 0; i < MaxInsertsPerFrame && _done.TryDequeue(out var chunk); i++)
        {
            var pos = new Vector2I(chunk.Cx, chunk.Cy);
            _inFlight.Remove(pos);

            // Kamera ist inzwischen weitergezogen: Ergebnis verwerfen.
            if (Math.Abs(pos.X - _center.X) > _rx + HysteresisX ||
                Math.Abs(pos.Y - _center.Y) > _ry + HysteresisY)
            {
                chunk.Dispose();
                continue;
            }

            if (!_world.Insert(chunk)) { chunk.Dispose(); continue; }
            ChunkLoaded?.Invoke(chunk);
        }

        if (!_done.IsEmpty) _rescan = true;      // Rest im nächsten Frame, danach auch neue Aufträge
        else if (_inFlight.Count > 0 || _toLoad.Count > 0) _rescan = true;
    }

    private void Unload()
    {
        int ux = _rx + HysteresisX, uy = _ry + HysteresisY;

        _toUnload.Clear();
        foreach (var pos in _world.Chunks.Keys)
            if (Math.Abs(pos.X - _center.X) > ux || Math.Abs(pos.Y - _center.Y) > uy)
                _toUnload.Add(pos);

        _toUnload.Sort((a, b) => Dist2(b).CompareTo(Dist2(a)));   // fernste zuerst

        int n = Math.Min(_toUnload.Count, MaxUnloadsPerFrame);
        for (int i = 0; i < n; i++)
        {
            var chunk = _world.GetChunk(_toUnload[i]);
            if (chunk == null) continue;
            ChunkUnloading?.Invoke(chunk);
            if (chunk.Modified) _store.Save(chunk);
            _world.UnloadChunk(_toUnload[i]);
        }
        if (_toUnload.Count > n) _rescan = true;
    }

    private void StartJobs()
    {
        _toLoad.Clear();
        for (int dy = -_ry; dy <= _ry; dy++)
        {
            for (int dx = -_rx; dx <= _rx; dx++)
            {
                var pos = new Vector2I(_center.X + dx, _center.Y + dy);
                if (_inFlight.Contains(pos) || _world.GetChunk(pos) != null) continue;
                _toLoad.Add(pos);
            }
        }

        _toLoad.Sort((a, b) => Dist2(a).CompareTo(Dist2(b)));     // nächste zuerst

        int started = 0;
        while (started < _toLoad.Count && _inFlight.Count < MaxInFlight)
        {
            var pos = _toLoad[started++];
            _inFlight.Add(pos);
            Task.Run(() =>
            {
                var chunk = new Chunk(pos.X, pos.Y);
                if (!_store.TryLoad(chunk))
                    _generator.Generate(chunk);
                _done.Enqueue(chunk);
            });
        }

        _toLoad.RemoveRange(0, started);                           // übrig = noch offen
        if (_toLoad.Count > 0) _rescan = true;
    }

    private int Dist2(Vector2I p)
    {
        int dx = p.X - _center.X, dy = p.Y - _center.Y;
        return dx * dx + dy * dy;
    }

    public void SaveAll()
    {
        foreach (var chunk in _world.Chunks.Values)
            if (chunk.Modified) { _store.Save(chunk); chunk.Modified = false; }
    }
}