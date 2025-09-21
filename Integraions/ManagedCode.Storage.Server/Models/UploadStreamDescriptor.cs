using System.Collections.Generic;

namespace ManagedCode.Storage.Server.Models;

/// <summary>
/// Describes the metadata associated with a streamed upload request.
/// </summary>
public class UploadStreamDescriptor
{
    /// <summary>
    /// Gets or sets the optional transfer identifier supplied by the caller.
    /// </summary>
    public string? TransferId { get; set; }
    /// <summary>
    /// Gets or sets the file name persisted to storage.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target directory.
    /// </summary>
    public string? Directory { get; set; }
    /// <summary>
    /// Gets or sets the MIME type associated with the upload.
    /// </summary>
    public string? ContentType { get; set; }
    /// <summary>
    /// Gets or sets the expected file size, if known.
    /// </summary>
    public long? FileSize { get; set; }
    /// <summary>
    /// Gets or sets optional metadata which will be forwarded to storage.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
