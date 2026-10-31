using System.Dynamic;
using Godot;

public enum MaterialType : byte { Empty, Solid, Powder, Liquid, Gas }

public struct MaterialDef
{
    public string Name;
    public MaterialType Type;
    public Color Color;
    public int Density;
}

public static class Materials
{
    public const ushort Empty = 0;
    public const ushort Sand = 1;
    public const ushort Water = 2;
    public const ushort Stone = 3;

    public static readonly MaterialDef[] Defs = new MaterialDef[]
    {
        new() { Name = "Empty", Type = MaterialType.Empty,  Color = new Color(0, 0, 0, 0),          Density = 0    },
        new() { Name = "Sand",  Type = MaterialType.Powder, Color = new Color(0.87f, 0.77f, 0.42f), Density = 1500 },
        new() { Name = "Water", Type = MaterialType.Liquid, Color = new Color(0.20f, 0.45f, 0.90f), Density = 1000 },
        new() { Name = "Stone", Type = MaterialType.Solid,  Color = new Color(0.45f, 0.45f, 0.48f), Density = 3000 }
    };

    public static readonly MaterialType[] Types = BuildTypes();
    public static readonly int[] Densities = BuildDensities();
    public static readonly uint[] ColorsRgba = BuildColors();

    private static MaterialType[] BuildTypes()
    {
        MaterialType[] t = new MaterialType[Defs.Length];
        for (int i = 0; i < Defs.Length; i++)
        {
            t[i] = Defs[i].Type;
        }
        return t;
    }

    private static int[] BuildDensities()
    {
        int[] d = new int[Defs.Length];
        for (int i = 0; i < Defs.Length; i++)
        {
            d[i] = Defs[i].Density;
        }
        return d;
    }

    private static uint[] BuildColors()
    {
        var c = new uint[Defs.Length];
        for (int i = 0; i < Defs.Length; i++)
        {
            Color col = Defs[i].Color;
            uint r = (uint)(col.R * 255);
            uint g = (uint)(col.G * 255);
            uint b = (uint)(col.B * 255);
            uint a = (uint)(col.A * 255);
            c[i] = r | (g << 8) | (b << 16) | (a << 24);
        }
        return c;
    }
}