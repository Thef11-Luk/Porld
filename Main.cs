using Godot;
using Porld;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Main : Node2D
{
	private const int BrushRadius = 10;
	private const bool ShowSleeping = true;   // schlafende Chunks abdunkeln (Diagnose)

	private readonly World _world = new();
	private readonly Dictionary<Vector2I, ChunkView> _views = new();

	private int _frame = 1;   // bei 1 starten: frisch erzeugte Zellen haben Stempel 0
	private ushort _brushMaterial = Materials.Sand;

	private Camera2D _camera = null!;
	private ChunkStreamer _streamer = null!;
	private readonly Stack<ChunkView> _viewPool = new();

	public override void _Ready()
	{
		_camera = GetNode<Camera2D>("Camera2D");     // Camera2D als Kind von Main anlegen
		_streamer = new ChunkStreamer(_world, new FlatGenerator(), new ChunkStore());

		_streamer.ChunkLoaded = chunk =>
		{
			ChunkView view;
			if (_viewPool.Count > 0) view = _viewPool.Pop();
			else { view = new ChunkView(); AddChild(view); }
			view.Init(chunk);
			_views[new Vector2I(chunk.Cx, chunk.Cy)] = view;
		};

		_streamer.ChunkUnloading = chunk =>
		{
			var pos = new Vector2I(chunk.Cx, chunk.Cy);
			if (!_views.Remove(pos, out var view)) return;
			view.Release();
			_viewPool.Push(view);
		};
	}

	public override void _Process(double delta)
	{
		// Teststeuerung der Kamera
		Vector2 move = Input.GetVector("a", "d", "w", "s");
		_camera.Position += move * (float)(600 * delta / _camera.Zoom.X);

		Vector2 halfView = GetViewportRect().Size / _camera.Zoom / 2f;
		_streamer.Update(_camera.GetScreenCenterPosition(), halfView);

		if (Input.IsKeyPressed(Key.Key1)) _brushMaterial = Materials.Sand;
		if (Input.IsKeyPressed(Key.Key2)) _brushMaterial = Materials.Water;
		if (Input.IsKeyPressed(Key.Key3)) _brushMaterial = Materials.Stone;
		if (Input.IsKeyPressed(Key.Key4)) _brushMaterial = Materials.Empty;

		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			Vector2 mouse = GetGlobalMousePosition();
			Paint(Mathf.FloorToInt(mouse.X), Mathf.FloorToInt(mouse.Y));
		}

		foreach (var view in _views.Values)
		{
			if (ShowSleeping)
				view.Modulate = !view.Chunk.Ready ? new Color(0.3f, 0.3f, 0.5f)
								: view.Chunk.IsAwake ? Colors.White
								: new Color(0.55f, 0.55f, 0.55f);

			if (!view.Chunk.NeedsRedraw) continue;
			view.Chunk.NeedsRedraw = false;
			view.Redraw();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		var sw = Stopwatch.StartNew();
		_world.Step(_frame);
		_frame++;
		sw.Stop();

		if (_frame % 60 == 0)
			GD.Print($"Sim: {sw.Elapsed.TotalMilliseconds:F3} ms");
	}

	private void Paint(int wx, int wy)
	{
		for (int dy = -BrushRadius; dy <= BrushRadius; dy++)
		{
			for (int dx = -BrushRadius; dx <= BrushRadius; dx++)
			{
				if (dx * dx + dy * dy > BrushRadius * BrushRadius) continue;
				SetCellWorld(wx + dx, wy + dy, _brushMaterial);
			}
		}
	}

	private void SetCellWorld(int wx, int wy, ushort material)
	{
		int cx = Mathf.FloorToInt(wx / (float)Chunk.Size);   // floor, wegen negativer Koordinaten
		int cy = Mathf.FloorToInt(wy / (float)Chunk.Size);

		var chunk = _world.GetChunk(new Vector2I(cx, cy));
		chunk?.SetMaterial(wx - cx * Chunk.Size, wy - cy * Chunk.Size, material);
	}

	public override void _ExitTree() => _streamer.SaveAll();
}