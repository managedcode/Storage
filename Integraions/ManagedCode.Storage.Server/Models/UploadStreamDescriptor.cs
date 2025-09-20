using System.Collections.Generic;

namespace ManagedCode.Storage.Server.Models;

public class UploadStreamDescriptor
{
    public string? TransferId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Directory { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
