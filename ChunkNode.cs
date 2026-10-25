using Godot;
using Porld;
using System;
using System.Diagnostics;

[GlobalClass]
public partial class ChunkNode : Sprite2D
{
	public Chunk Chunk;
	private Image _chunkImage;
	private ImageTexture _chunkTexture;
	private ShaderMaterial _shaderMaterial;

	public ChunkNode(Chunk chunk)
	{
		Chunk = chunk;
	}
	public override void _Ready()
	{
		_chunkImage = Image.CreateFromData(Chunk.Width, Chunk.Height, false, Image.Format.R8, Chunk.Pixels);
		_chunkTexture = ImageTexture.CreateFromImage(_chunkImage);

		Texture = _chunkTexture;

		Shader shader = GD.Load<Shader>("res://chunk_shader.gdshader");
		_shaderMaterial = new ShaderMaterial();
		_shaderMaterial.Shader = shader;

		_shaderMaterial.SetShaderParameter("chunk_texture", _chunkTexture);

		Material = _shaderMaterial;
		Centered = false;
	}

	public override void _Process(double delta)
	{
		if (Chunk.IsDirty)
		{
			Chunk.IsDirty = false;

			_chunkImage.SetData(Chunk.Width, Chunk.Height, false, Image.Format.R8, Chunk.Pixels);
			_chunkTexture.Update(_chunkImage);

			Vector2 worldPos = new Vector2(Chunk.ChunkCoords.X * Chunk.Width, Chunk.ChunkCoords.Y * Chunk.Height);
			_shaderMaterial.SetShaderParameter("chunk_world_pos", worldPos);
		}
	}
}
