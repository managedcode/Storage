using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Server.Models;

/// <summary>
/// Represents the status of a streaming transfer processed by the storage hub.
/// </summary>
public class TransferStatus
{
    /// <summary>
    /// Gets or sets the unique identifier associated with the transfer.
    /// </summary>
    public string TransferId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type (e.g. upload, download).
    /// </summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical resource name involved in the transfer.
    /// </summary>
    public string? ResourceName { get; init; }

    /// <summary>
    /// Gets or sets the cumulative number of bytes processed.
    /// </summary>
    public long BytesTransferred { get; set; }

    /// <summary>
    /// Gets or sets the total number of bytes expected, when known.
    /// </summary>
    public long? TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the transfer completed successfully.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the transfer was canceled.
    /// </summary>
    public bool IsCanceled { get; set; }

    /// <summary>
    /// Gets or sets error details when the transfer fails.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the metadata returned by the storage provider after upload.
    /// </summary>
    public BlobMetadata? Metadata { get; set; }
}
