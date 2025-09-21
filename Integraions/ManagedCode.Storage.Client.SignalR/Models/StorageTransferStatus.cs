using System.Text.Json.Serialization;

namespace ManagedCode.Storage.Client.SignalR.Models;

/// <summary>
/// Represents the status of a storage transfer reported by the SignalR hub.
/// </summary>
public class StorageTransferStatus
{
    /// <summary>
    /// Gets or sets the transfer identifier supplied by the server.
    /// </summary>
    [JsonPropertyName("transferId")]
    public string TransferId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transfer operation type (upload/download).
    /// </summary>
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical resource name related to the transfer.
    /// </summary>
    [JsonPropertyName("resourceName")]
    public string? ResourceName { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes processed so far.
    /// </summary>
    [JsonPropertyName("bytesTransferred")]
    public long BytesTransferred { get; set; }

    /// <summary>
    /// Gets or sets the total bytes expected, when provided by the server.
    /// </summary>
    [JsonPropertyName("totalBytes")]
    public long? TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the transfer completed successfully.
    /// </summary>
    [JsonPropertyName("isCompleted")]
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the transfer was canceled.
    /// </summary>
    [JsonPropertyName("isCanceled")]
    public bool IsCanceled { get; set; }

    /// <summary>
    /// Gets or sets the error message associated with a failed transfer.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the blob metadata returned after a successful upload.
    /// </summary>
    [JsonPropertyName("metadata")]
    public BlobMetadataDto? Metadata { get; set; }
}

/// <summary>
/// Lightweight metadata returned by the storage provider.
/// </summary>
public class BlobMetadataDto
{
    /// <summary>
    /// Gets or sets the blob name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified blob name.
    /// </summary>
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the MIME type recorded by the server.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the container/bucket name.
    /// </summary>
    [JsonPropertyName("container")]
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the blob length in bytes.
    /// </summary>
    [JsonPropertyName("length")]
    public ulong Length { get; set; }
}
