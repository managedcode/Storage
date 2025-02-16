namespace ManagedCode.Storage.Server.Models;

public class FilePayload
{
    public int ChunkIndex { get; set; }
    public int ChunkSize { get; set; }
}