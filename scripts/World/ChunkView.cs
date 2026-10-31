using System.Runtime.CompilerServices;
using Godot;
using Porld;

public unsafe partial class ChunkView : Sprite2D
{
    private Image _image = null;
    private ImageTexture _texture = null;
    private readonly byte[] _buffer = new byte[UnsafeChunk.CellCount * 4];

    public UnsafeChunk Chunk = null;

    public void Init(UnsafeChunk chunk)
    {
        Chunk = chunk;
        Centered = false;
        TextureFilter = TextureFilterEnum.Nearest;
        Position = new Vector2(chunk.Cx * UnsafeChunk.Size, chunk.Cy * UnsafeChunk.Size);

        _image = Image.CreateEmpty(UnsafeChunk.Size, UnsafeChunk.Size, false, Image.Format.Rgba8);
        _texture = ImageTexture.CreateFromImage(_image);
        Texture = _texture;
    }

    public void Redraw()
    {
        Cell* cells = Chunk.Cells;
        for (int i = 0; i < UnsafeChunk.CellCount; i++)
        {
            Color c = Materials.Defs[cells[i].Material].Color;
            _buffer[i * 4 + 0] = (byte)(c.R * 255);
            _buffer[i * 4 + 1] = (byte)(c.G * 255);
            _buffer[i * 4 + 2] = (byte)(c.B * 255);
            _buffer[i * 4 + 3] = (byte)(c.A * 255);
        }

        _image.SetData(UnsafeChunk.Size, UnsafeChunk.Size, false, Image.Format.Rgba8, _buffer);
        _texture.Update(_image);
    }
}