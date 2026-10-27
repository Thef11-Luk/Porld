using System;
using Godot;
using Porld;

namespace Porld;

public class TerrainGenerator
{
    public const int SurfaceY = 400;
    public const int SeaLevel = 440;
    private FastNoiseLite _surfaceNoise;
    private FastNoiseLite _caveNoise;
    private FastNoiseLite _dirtNoise;
    private FastNoiseLite _sandNoise;
    private int _seed;

    public TerrainGenerator(int seed = 1337)
    {
        _seed = seed;

        // noise for hills
        _surfaceNoise = new FastNoiseLite();
        _surfaceNoise.Seed = seed;
        _surfaceNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _surfaceNoise.Frequency = 0.0005f;

        // noise for caves
        _caveNoise = new FastNoiseLite();
        _caveNoise.Seed = seed + 1;
        _caveNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _caveNoise.Frequency = 0.001f;

        // noise for dirt
        _dirtNoise = new FastNoiseLite();
        _dirtNoise.Seed = seed + 2;
        _dirtNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _dirtNoise.Frequency = 0.05f;

        // noise for sand
        _sandNoise = new FastNoiseLite();
        _sandNoise.Seed = seed + 3;
        _sandNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _sandNoise.Frequency = 0.05f;
    }

    public void GenerateChunk(Chunk chunk)
    {
        int chunkHeightInWorld = chunk.ChunkCoords.Y * chunk.Height;
        int chunkWidthInWorld = chunk.ChunkCoords.X * chunk.Width;

        for (int x = 0; x < chunk.Width; x++)
        {
            int globalX = chunkWidthInWorld + x;

            float surfaceNoiseVal = _surfaceNoise.GetNoise1D(globalX);
            int surfaceY = SurfaceY + (int)(surfaceNoiseVal * (double)SurfaceY);

            for (int y = 0; y < chunk.Height; y++)
            {
                int globalY = chunkHeightInWorld + y;

                // every pixel above surface is empty
                if (globalY < surfaceY)
                {
                    if (globalY >= SeaLevel)
                    {
                        chunk.SetPixel(x, y, Pixel.Water);
                    }
                    continue;
                }

                // every pixel in a cave is empty
                int depthBelowSurface = globalY - surfaceY;

                if (depthBelowSurface > 50)
                {
                    float caveVal = _caveNoise.GetNoise2D(globalX, globalY);


                    float transitionFactor = Mathf.Clamp((depthBelowSurface - 50) / 35.0f, 0.0f, 1.0f);
                    float currentThreshold = 0.1f * transitionFactor;


                    if (caveVal > -currentThreshold && caveVal < currentThreshold)
                    {
                        continue;
                    }
                }

                float belowSeaLevel = Math.Clamp((surfaceY - SeaLevel) / 5, 0.0f, 1.0f);
                float sandNoiseVal = _sandNoise.GetNoise1D(globalX);
                int dynamicSandThickness = Math.Max(3 + (int)(sandNoiseVal * belowSeaLevel * 30.0f), 1);
                if (globalY >= surfaceY && surfaceY > SeaLevel && globalY > SeaLevel && globalY <= surfaceY + dynamicSandThickness)
                {
                    chunk.SetPixel(x, y, Pixel.WetSand);
                }
                else
                {
                    float dirtNoiseVal = _dirtNoise.GetNoise1D(globalX);
                    int dynamicDirtThickness = 30 + (int)(dirtNoiseVal * 12.0f);
                    if (globalY == surfaceY)
                    {
                        chunk.SetPixel(x, y, Pixel.Grass);
                    }
                    else if (globalY < surfaceY + dynamicDirtThickness)
                    {
                        chunk.SetPixel(x, y, Pixel.Dirt);
                    }
                    else
                    {
                        chunk.SetPixel(x, y, Pixel.Stone);
                    }
                }



            }
        }
    }
}
