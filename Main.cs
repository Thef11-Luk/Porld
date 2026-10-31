using Godot;
using Porld;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

public unsafe partial class Main : Node2D
{
	private readonly World _world = new();
	private readonly Dictionary<Vector2I, ChunkView> _views = new();

	private int _frame = 0;

	private ushort _brushMaterial = Materials.Sand;
	private const int BrushRadius = 10;

	public override void _Ready()
	{
		for (int y = 0; y < 3; y++)
		{
			for (int x = 0; x < 3; x++)
			{
				var chunk = _world.LoadChunk(new Vector2I(x, y));
				var view = new ChunkView();
				AddChild(view);
				view.Init(chunk);
				_views[new Vector2I(x, y)] = view;
			}
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.Key1)) _brushMaterial = Materials.Sand;
		if (Input.IsKeyPressed(Key.Key2)) _brushMaterial = Materials.Water;
		if (Input.IsKeyPressed(Key.Key3)) _brushMaterial = Materials.Stone;
		if (Input.IsKeyPressed(Key.Key4)) _brushMaterial = Materials.Empty;

		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			Vector2 mouse = GetGlobalMousePosition();
			Paint((int)Mathf.Floor(mouse.X), (int)Mathf.Floor(mouse.Y));
		}

		foreach (var view in _views.Values)
		{
			if (!view.Chunk.NeedsRedraw) continue;
			view.Chunk.NeedsRedraw = false;
			view.Redraw();
		}
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
		int cx = Mathf.FloorToInt(wx / (float)Chunk.Size);
		int cy = Mathf.FloorToInt(wy / (float)Chunk.Size);

		var chunk = _world.GetChunk(new Vector2I(cx, cy));
		if (chunk == null) return;

		int lx = wx - cx * Chunk.Size;
		int ly = wy - cy * Chunk.Size;
		chunk.Cells[ly * Chunk.Size + lx].Material = material;
		chunk.MarkDirty(lx, ly);
	}

    public override void _PhysicsProcess(double delta)
    {
		Stopwatch sw = new();
		sw.Start();

		foreach (var chunk in _world.Chunks.Values)
		{
			chunk.BeginFrame();
		}

		var ordered = _world.Chunks.Values.OrderByDescending(c => c.Cy);
        foreach (var chunk in ordered)
		{
			Simulator.StepChunk(chunk, _frame);
		}
		_frame++;
		sw.Stop();
		GD.Print(sw.ElapsedMilliseconds);
    }
}