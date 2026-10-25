using Godot;
using System.Text;

namespace Porld;

public enum Pixel : byte
{
    Empty,
    Sand,
	Stone,
	Grass,
	Dirt
}
public class Chunk
{
    public readonly byte Width, Height;
	public readonly Vector2I ChunkCoords;
    public byte[] Pixels;
	public bool IsDirty = true;

	private int _inactiveTicks = 0;
	private const int SleepThreshold = 30;



    public Chunk(byte width, byte height, Vector2I chunkCoords)
    {
        Width = width;
        Height = height;
        Pixels = new byte[height * width];
		ChunkCoords = chunkCoords;
    }

	private int GetIndex(int x, int y) => y * Width + x;

	private bool Inside(int x, int y)
    {
        return y < Height && y >= 0 && x < Width && x >= 0;
    }

	public Pixel GetPixel(int x, int y)
    {
        return (Pixel)Pixels[GetIndex(x, y)];
    }

	private Pixel GetPixelGlobal(int x, int y)
	{
		if (Inside(x, y))
		{
			return GetPixel(x, y);
		}
		int globalX = ChunkCoords.X * Width + x;
		int globalY = ChunkCoords.Y * Height + y;
		return WorldManager.Instance.GetPixelGlobal(globalX, globalY);
	}

	public void SetPixel(int x, int y, Pixel pixel)
    {
		_inactiveTicks = 0;
		IsDirty = true;
        Pixels[GetIndex(x, y)] = (byte)pixel;
    } 

	private void SetPixelGlobal(int x, int y, Pixel pixel)
	{
		if (Inside(x, y))
		{
			SetPixel(x, y, pixel);
			return;
		}
		int globalX = ChunkCoords.X * Width + x;
		int globalY = ChunkCoords.Y * Height + y;
		WorldManager.Instance.SetPixelGlobal(globalX, globalY, pixel);
	}

    public void Simulate()
    {
		if (_inactiveTicks >= SleepThreshold)
		{
			return;
		}

		_inactiveTicks++;
        for(int y = Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < Width; x++)
            {
                SimulatePixel(y, x);
            }
        }
    }

	public byte[] GetRawBytes()
	{
		return Pixels;
	}

    private void SimulatePixel(int y, int x)
    {
        switch (GetPixel(x, y))
        {
            case Pixel.Sand:
                {
					if(GetPixelGlobal(x, y + 1) == Pixel.Empty)
					{
						SetPixelGlobal(x, y + 1, Pixel.Sand);
						SetPixel(x, y, Pixel.Empty);
					}
					else if (GetPixelGlobal(x - 1, y + 1) == Pixel.Empty)
					{
						SetPixelGlobal(x - 1, y + 1, Pixel.Sand);
						SetPixel(x, y, Pixel.Empty);
					}
					else if (GetPixelGlobal(x + 1, y + 1) == Pixel.Empty)
					{
						SetPixelGlobal(x + 1, y + 1, Pixel.Sand);
						SetPixel(x, y, Pixel.Empty);
					}

                    break;
                }
        }
    }

    public override string ToString()
    {
        StringBuilder asStringBuilder = new StringBuilder();

        for(int y = 0; y < Height; y++)
        {
            for(int x = 0; x < Width; x++)
            {
                asStringBuilder.Append(Pixels[GetIndex(x, y)]);
            }
            if (y < Height - 1)
            {
                asStringBuilder.AppendLine();
            }
        }

        return asStringBuilder.ToString();
    }
}
