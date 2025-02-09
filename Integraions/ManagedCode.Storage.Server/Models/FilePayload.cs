namespace ManagedCode.Storage.Server;

public class FilePayload
{
    public int ChunkIndex { get; set; }
    public int ChunkSize { get; set; }
}