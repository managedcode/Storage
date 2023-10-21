using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Server;

public class FileUploadPayload
{
    public IFormFile File { get; set; }
    public FilePayload Payload { get; set; }
}

public class FilePayload
{
    public string? BlobName { get; set; }
    public int ChunkIndex { get; set; }
    public int ChunkSize { get; set; }
    public uint FullCRC { get; set; }
    public string? UploadedCRC { get; set; }
}