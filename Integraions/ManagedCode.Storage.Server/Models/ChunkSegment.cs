namespace ManagedCode.Storage.Server.Models;

public class ChunkSegment
{
    public string UploadId { get; set; } = string.Empty;
    public int Index { get; set; }
    public int TotalChunks { get; set; }
    public int Size { get; set; }
    public long? FileSize { get; set; }
    public byte[] Data { get; set; } = []; // assumes base64 from client
}
