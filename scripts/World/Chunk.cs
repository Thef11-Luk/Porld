using Godot;
using System;
using System.Linq;
using System.Text;

namespace Porld;

public enum Pixel : byte
{
    Empty,
    Sand,
	Stone,
	Grass,
	Dirt,
	Water
}
public class Chunk
{
    public readonly byte Width, Height;
	public readonly Vector2I ChunkCoords;
    public byte[] Pixels;
	public bool IsDirty = true;

	private int _inactiveTicks = 0;
	private const int SleepThreshold = 1;
	private bool _isEvenTick = true;



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

	private void SwapPixels(int x1, int y1, int x2, int y2)
	{
		Pixel temp = GetPixelGlobal(x1, y1);
		SetPixelGlobal(x1, y1, GetPixelGlobal(x2, y2));
		SetPixelGlobal(x2, y2, temp);
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
		_isEvenTick = !_isEvenTick;
    }



    private void SimulatePixel(int y, int x)
    {
		bool chess = (y + x) % 2 == 0;
        switch (GetPixel(x, y))
        {
            case Pixel.Sand:
                {
					if(new Pixel[]{Pixel.Empty, Pixel.Water}.Contains(GetPixelGlobal(x, y + 1)))
					{
						SwapPixels(x, y, x, y + 1);
					}
					else if (new Pixel[]{Pixel.Empty, Pixel.Water}.Contains(GetPixelGlobal(x - 1, y + 1)))
					{
						SwapPixels(x, y, x - 1, y + 1);
					}
					else if (new Pixel[]{Pixel.Empty, Pixel.Water}.Contains(GetPixelGlobal(x + 1, y + 1)))
					{
						SwapPixels(x, y, x + 1, y + 1);
					}

                    break;
				}
			case Pixel.Water:
				{
					int[] order;
					if (_isEvenTick)
					{
						order = [-1, 1];
					} else
					{
						order = [1, -1];
					}

					if (GetPixelGlobal(x, y + 2) == Pixel.Empty)
					{
						SetPixelGlobal(x, y + 1, Pixel.Water);
						SetPixel(x, y, Pixel.Empty);
					}
					else
					{
						foreach (int offset in order)
						{
							if (GetPixelGlobal(x + offset, y + 1) == Pixel.Empty)
							{
								SetPixelGlobal(x + offset, y + 1, Pixel.Water);
								SetPixelGlobal(x, y, Pixel.Empty);
								break;
							}
							if (GetPixelGlobal(x + offset, y) == Pixel.Empty)
							{
								SetPixelGlobal(x + offset, y, Pixel.Water);
								SetPixelGlobal(x, y, Pixel.Empty);
								break;
							}

						}
					}
				}


				break;
		}

	}

	public byte[] GetRawBytes()
	{
		return Pixels;
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
