using Godot;
using Porld;

public unsafe partial class ChunkView : Sprite2D
{
    private Image _image = null!;
    private ImageTexture _texture = null!;
    private readonly byte[] _buffer = new byte[Chunk.CellCount * 4];   // RGBA

    public Chunk Chunk = null!;

    public void Init(Chunk chunk)
    {
        Chunk = chunk;
        Centered = false;
        TextureFilter = TextureFilterEnum.Nearest;
        Position = new Vector2(chunk.Cx * Chunk.Size, chunk.Cy * Chunk.Size);
        Modulate = Colors.White;
        Visible = true;

        if (_image == null)   // nur beim allerersten Mal anlegen, danach wiederverwenden
        {
            _image = Image.CreateEmpty(Chunk.Size, Chunk.Size, false, Image.Format.Rgba8);
            _texture = ImageTexture.CreateFromImage(_image);
            Texture = _texture;
        }
        chunk.NeedsRedraw = true;
    }

    public void Release()
    {
        Visible = false;
        Chunk = null!;
    }

    public void Redraw()
    {
        Cell* cells = Chunk.Cells;
        uint[] colors = Materials.ColorsRgba;

        fixed (byte* p = _buffer)
        {
            uint* px = (uint*)p;
            for (int i = 0; i < Chunk.CellCount; i++)
                px[i] = colors[cells[i].Material];
        }

        _image.SetData(Chunk.Size, Chunk.Size, false, Image.Format.Rgba8, _buffer);
        _texture.Update(_image);
    }
}