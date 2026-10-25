using System;
using Godot;
using Porld;

namespace Porld;

public class TerrainGenerator
{
    private FastNoiseLite _surfaceNoise;
    private FastNoiseLite _caveNoise;
    private FastNoiseLite _dirtNoise;
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
    }

    public void GenerateChunk(Chunk chunk)
    {
        int chunkHeightInWorld = chunk.ChunkCoords.Y * chunk.Height;
        int chunkWidthInWorld = chunk.ChunkCoords.X * chunk.Width;

        for (int x = 0; x < chunk.Width; x++)
        {
            int globalX = chunkWidthInWorld + x;

            float surfaceNoiseVal = _surfaceNoise.GetNoise1D(globalX);
            int surfaceY = 400 + (int)(surfaceNoiseVal * 400.0f);

            for (int y = 0; y < chunk.Height; y++)
            {
                int globalY = chunkHeightInWorld + y;

                // every pixel above surface is empty
                if (globalY < surfaceY)
                {
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


                float dirtNoiseVal = _dirtNoise.GetNoise1D(globalX);
                int dynamicDirtThickness = 30 + (int)(dirtNoiseVal * 12.0f);

                if (globalY == surfaceY)
                {
                    chunk.SetPixel(x, y, Pixel.Grass);
                }
                else if (globalY < surfaceY + dynamicDirtThickness)
                {
                    chunk.SetPixel(x, y, Pixel.Dirt);
                } else
                {
                    chunk.SetPixel(x, y, Pixel.Stone);
                }


            }
        }
    }
}
