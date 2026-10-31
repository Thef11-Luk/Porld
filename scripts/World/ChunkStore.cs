using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using Godot;
using Porld;

// Hält komprimierte Chunks im RAM. Später lässt sich der Speicher gegen Dateien tauschen.
public sealed unsafe class ChunkStore
{
    private readonly ConcurrentDictionary<Vector2I, byte[]> _data = new();

    public int Count => _data.Count;

    public void Save(Chunk chunk)
        => _data[new Vector2I(chunk.Cx, chunk.Cy)] = Encode(chunk);

    // Füllt den Chunk aus dem Speicher. false = nichts gespeichert, Generator ist dran.
    public bool TryLoad(Chunk chunk)
    {
        if (!_data.TryGetValue(new Vector2I(chunk.Cx, chunk.Cy), out var bytes)) return false;
        return Decode(bytes, chunk);
    }

    // Format: Folge von Läufen, je 4 Byte Wert (Material | State<<16) + 2 Byte Länge.
    // Das Stempel-Byte (Flags) wird bewusst nicht gespeichert.
    private static byte[] Encode(Chunk chunk)
    {
        Span<byte> buf = stackalloc byte[Chunk.CellCount * 6];   // Worst Case: jede Zelle ein Lauf
        Cell* cells = chunk.Cells;
        int pos = 0;
        int i = 0;

        while (i < Chunk.CellCount)
        {
            uint key = Key(cells[i]);
            int run = 1;
            while (i + run < Chunk.CellCount && Key(cells[i + run]) == key) run++;

            BinaryPrimitives.WriteUInt32LittleEndian(buf.Slice(pos), key);
            BinaryPrimitives.WriteUInt16LittleEndian(buf.Slice(pos + 4), (ushort)run);
            pos += 6;
            i += run;
        }
        return buf.Slice(0, pos).ToArray();
    }

    private static bool Decode(byte[] bytes, Chunk chunk)
    {
        Cell* cells = chunk.Cells;
        int i = 0;

        for (int pos = 0; pos + 6 <= bytes.Length; pos += 6)
        {
            uint key = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pos));
            int run = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(pos + 4));
            if (i + run > Chunk.CellCount) return false;          // beschädigte Daten

            for (int k = 0; k < run; k++, i++)
            {
                cells[i].Material = (ushort)key;
                cells[i].State = (byte)(key >> 16);
                cells[i].Flags = 0;
            }
        }
        return i == Chunk.CellCount;
    }

    private static uint Key(Cell c) => (uint)c.Material | ((uint)c.State << 16);
}