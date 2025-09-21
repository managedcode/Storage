using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ManagedCode.Storage.Client.SignalR.Models;

/// <summary>
/// Describes the payload associated with a streaming upload request.
/// </summary>
public class StorageUploadStreamDescriptor
{
    /// <summary>
    /// Gets or sets the client-specified transfer identifier.
    /// </summary>
    [JsonPropertyName("transferId")]
    public string? TransferId { get; set; }

    /// <summary>
    /// Gets or sets the file name stored in the backing storage.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional directory or folder path.
    /// </summary>
    [JsonPropertyName("directory")]
    public string? Directory { get; set; }

    /// <summary>
    /// Gets or sets the MIME type associated with the upload.
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the expected file size in bytes.
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    /// <summary>
    /// Gets or sets optional metadata forwarded to the storage provider.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}
