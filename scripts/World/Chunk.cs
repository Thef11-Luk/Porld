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
	Water,
	WetSand,
	OutOfBounds
}
public class Chunk
{
    public readonly byte Width, Height;
	public readonly Vector2I ChunkCoords;
    public byte[] Pixels;
	public bool IsDirty = true;

	public int InactiveTicks = 0;
	private const int SleepThreshold = 5;

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
		InactiveTicks = 0;
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
		if (InactiveTicks >= SleepThreshold)
		{
			return;
		}

		InactiveTicks++;
		int start = WorldManager.Instance.IsEvenTick ? 0 : Width - 1;
		int end = WorldManager.Instance.IsEvenTick ? Width - 1 : 0;
		int change = WorldManager.Instance.IsEvenTick ? 1 : -1;
		for (int y = Height - 1; y >= 0; y--)
        {

            for (int x = start; x != end; x += change)
            {
                SimulatePixel(y, x);
            }
        }
    }



    private void SimulatePixel(int y, int x)
    {
        switch (GetPixel(x, y))
        {
            case Pixel.Sand:
                {
					if(new Pixel[]{Pixel.Empty, Pixel.Water}.Contains(GetPixelGlobal(x, y + 1)))
					{
						if (GetPixelGlobal(x, y + 1) == Pixel.Water)
						{
							SetPixelGlobal(x, y + 1, Pixel.WetSand);
							SetPixelGlobal(x, y, Pixel.Empty);
						}
						else
							SwapPixels(x, y, x, y + 1);
					}
					else if (new Pixel[]{Pixel.Empty, Pixel.Water}.Contains(GetPixelGlobal(x - 1, y + 1)))
					{
						if (GetPixelGlobal(x - 1, y + 1) == Pixel.Water)
						{
							SetPixelGlobal(x - 1, y + 1, Pixel.WetSand);
							SetPixelGlobal(x, y, Pixel.Empty);
						}
						else
							SwapPixels(x, y, x - 1, y + 1);
					}
					else if (new Pixel[]{Pixel.Empty, Pixel.Water}.Contains(GetPixelGlobal(x + 1, y + 1)))
					{
						if (GetPixelGlobal(x + 1, y + 1) == Pixel.Water)
						{
							SetPixelGlobal(x + 1, y + 1, Pixel.WetSand);
							SetPixelGlobal(x, y, Pixel.Empty);
						}
						else
							SwapPixels(x, y, x + 1, y + 1);
					}

                    break;
				}
			case Pixel.Water:
				{
					int[] order;
					if ( WorldManager.Instance.IsEvenTick)
					{
						order = [-1, 1];
					} else
					{
						order = [1, -1];
					}


					if (GetPixelGlobal(x, y + 2) == Pixel.Empty && GetPixelGlobal(x, y + 1) == Pixel.Empty)
					{
						SetPixelGlobal(x, y + 1, Pixel.Water);
						SetPixelGlobal(x, y, Pixel.Empty);
					}
					else
					{
						bool pixelSet = false;
						foreach (int offset in order)
						{
							if (GetPixelGlobal(x + offset, y + 1) == Pixel.Empty)
							{
								SetPixelGlobal(x + offset, y + 1, Pixel.Water);
								SetPixelGlobal(x, y, Pixel.Empty);
								pixelSet = true;
								break;
							}
						}
						if (pixelSet) break;

						foreach (int offset in order)
						{
							if (GetPixelGlobal(x + offset, y) == Pixel.Empty)
							{
								SetPixelGlobal(x + offset, y, Pixel.Water);
								SetPixelGlobal(x, y, Pixel.Empty);
								break;
							}
						}
					}
					break;
				}
			case Pixel.WetSand:
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

		}

	}

	public bool IsActive()
	{
		return InactiveTicks < SleepThreshold;
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
