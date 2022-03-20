using System;

namespace ManagedCode.Storage.Core.Models;

public class BlobMetadata
{
    public string Name { get; set; } = null!;
    public Uri Uri { get; set; } = null!;
    public string? Container { get; set; }
    public string? ContentType { get; set; }
    public bool Rewrite { get; set; }
}