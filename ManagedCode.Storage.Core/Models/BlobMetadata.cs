using System;
using System.Collections.Generic;

namespace ManagedCode.Storage.Core.Models;

public class BlobMetadata
{
    public string FullName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Uri? Uri { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string? Container { get; set; }
    public string? MimeType { get; set; }
    public ulong Length { get; set; }
}