namespace ManagedCode.Storage.Server.Models;

public class FilePayload
{
    public string UploadId { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public int ChunkIndex { get; set; }
    public int ChunkSize { get; set; }
    public int TotalChunks { get; set; }
}
