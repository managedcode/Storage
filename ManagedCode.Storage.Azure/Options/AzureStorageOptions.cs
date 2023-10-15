using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Azure.Options;

public class AzureStorageOptions : IAzureStorageOptions
{
    public string? ConnectionString { get; set; }
    public string? Container { get; set; }
    public PublicAccessType PublicAccessType { get; set; }
    public BlobClientOptions? OriginalOptions { get; set; }

    public bool CreateContainerIfNotExists { get; set; } = true;
}