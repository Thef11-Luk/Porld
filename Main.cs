using Godot;
using System;
using Porld;

public partial class Main : Node2D
{
	private Camera2D cam;
	public override void _Ready()
	{
		cam = GetNode<Camera2D>("Camera2D");

		WorldManager.Instance.ChunkLoaded += OnChunkLoaded;		
		WorldManager.Instance.ChunkUnloaded += OnChunkUnloaded;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		WorldManager.Instance.UpdateChunkStreaming(cam.Position);

		if(Input.IsMouseButtonPressed(MouseButton.Left))
		{
			Vector2I mousePos = (Vector2I)GetLocalMousePosition();
			WorldManager.Instance.SetPixelGlobal(mousePos.X, mousePos.Y, Pixel.Sand);
		}
		if(Input.IsMouseButtonPressed(MouseButton.Right))
		{
			Vector2I mousePos = (Vector2I)GetLocalMousePosition();
			WorldManager.Instance.SetPixelGlobal(mousePos.X, mousePos.Y, Pixel.Water);
		}
		if(Input.IsMouseButtonPressed(MouseButton.Middle))
		{
			Vector2I mousePos = (Vector2I)GetLocalMousePosition();
			int radius = 5;
			for (int x = -radius; x <= radius; x++)
			{
				for (int y = -radius; y <= radius; y++)
				{
					WorldManager.Instance.SetPixelGlobal(mousePos.X + x, mousePos.Y + y, Pixel.Empty);
				}
			}
		}
		
		if (Input.IsKeyPressed(Key.Up))
		{
			cam.Position -= new Vector2(0, 4);
		}
		if (Input.IsKeyPressed(Key.Down))
		{
			cam.Position += new Vector2(0, 4);
		}
		if (Input.IsKeyPressed(Key.Left))
		{
			cam.Position -= new Vector2(4, 0);
		}
		if (Input.IsKeyPressed(Key.Right))
		{
			cam.Position += new Vector2(4, 0);
		}
	}

	private void OnChunkLoaded(Chunk chunk)
	{
		ChunkNode chunkNode = new ChunkNode(chunk);
		chunkNode.Position = chunk.ChunkCoords * 64;
		AddChild(chunkNode);
		GD.Print("Chunk added");
	}

	private void OnChunkUnloaded(Chunk chunk)
	{
		foreach(var node in GetChildren())
		{
			if (node is ChunkNode chunkNode)
			{
				if (chunkNode.Chunk == chunk)
				{
					node.QueueFree();
					RemoveChild(node);
				}
			}
		}
	}
}
