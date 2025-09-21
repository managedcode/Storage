using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Server.Models;

public class ChunkUploadCompleteResponse
{
    public uint Checksum { get; set; }
    public BlobMetadata? Metadata { get; set; }
}
