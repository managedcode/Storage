using System;

namespace ManagedCode.Storage.Core.Models;

public class BlobMetadata
{
    public string Name { get; set; }
    public Uri Uri { get; set; }

    public string Container { get; set; }
}