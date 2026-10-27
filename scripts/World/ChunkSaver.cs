using Godot;
using Porld;
using System;
using System.IO;

public class ChunkSaver
{
    private string GetChunkPath(string worldName, Chunk chunk)
    {
        string dir = $"user://saves/{worldName}/chunks/";
        DirAccess.MakeDirRecursiveAbsolute(dir);

        return ProjectSettings.GlobalizePath($"{dir}chunk_{chunk.ChunkCoords.X}_{chunk.ChunkCoords.Y}.bin");
    }

    public void SaveChunk(string worldName, Chunk chunk)
    {
        string path = $"user://saves/{worldName}/chunks/chunk_{chunk.ChunkCoords.X}_{chunk.ChunkCoords.Y}.bin";
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(path.GetBaseDir()));
        
        using var file = Godot.FileAccess.OpenCompressed(path, Godot.FileAccess.ModeFlags.Write, Godot.FileAccess.CompressionMode.Zstd);
        if (file != null)
        {
            file.StoreBuffer(chunk.Pixels);
        }
    }

    public bool TryLoadChunk(string worldName, Chunk chunk)
    {
        string path = $"user://saves/{worldName}/chunks/chunk_{chunk.ChunkCoords.X}_{chunk.ChunkCoords.Y}.bin";

        if (!Godot.FileAccess.FileExists(path)) return false;

        using var file = Godot.FileAccess.OpenCompressed(path, Godot.FileAccess.ModeFlags.Read, Godot.FileAccess.CompressionMode.Zstd);
        if (file != null)
        {
            chunk.Pixels = file.GetBuffer((long)(64 * 64));
            return true;
        }

        return false;
    }
}
