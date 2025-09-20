using System.Collections.Generic;

namespace ManagedCode.Storage.Server.Models;

public class ChunkUploadCompleteRequest
{
    public string UploadId { get; set; } = default!;
    public string? FileName { get; set; }
    public string? Directory { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public bool CommitToStorage { get; set; } = true;
    public bool KeepMergedFile { get; set; }
}
