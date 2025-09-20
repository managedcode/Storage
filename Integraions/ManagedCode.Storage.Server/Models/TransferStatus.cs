using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Server.Models;

public class TransferStatus
{
    public string TransferId { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public string? ResourceName { get; init; }
    public long BytesTransferred { get; set; }
    public long? TotalBytes { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsCanceled { get; set; }
    public string? Error { get; set; }
    public BlobMetadata? Metadata { get; set; }
}
