using Godot;
using Porld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public unsafe partial class Main : Node2D
{
	private readonly World _world = new();
	private readonly Dictionary<Vector2I, ChunkView> _views = new();

	private ushort _brushMaterial = Materials.Sand;
	private const int BrushRadius = 4;

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
		int cx = Mathf.FloorToInt(wx / (float)UnsafeChunk.Size);
		int cy = Mathf.FloorToInt(wy / (float)UnsafeChunk.Size);

		var chunk = _world.GetChunk(new Vector2I(cx, cy));
		if (chunk == null) return;

		int lx = wx - cx * UnsafeChunk.Size;
		int ly = wy - cy * UnsafeChunk.Size;
		chunk.Cells[ly * UnsafeChunk.Size + lx].Material = material;
	}
}