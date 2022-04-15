using System;

namespace ManagedCode.Storage.Core.Models;

public class BlobMetadata
{
    public string Name { get; set; } = null!;
    public Uri? Uri { get; set; }
    public string? Container { get; set; }
    public string? ContentType { get; set; }
    public bool Rewrite { get; set; }
    public long Length { get; set; }
}