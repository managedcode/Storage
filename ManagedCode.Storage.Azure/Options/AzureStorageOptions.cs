using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Azure.Options;

public class AzureStorageOptions : StorageOptions
{
    public string? ConnectionString { get; set; }
    public string? Container { get; set; }
    public PublicAccessType PublicAccessType { get; set; }
    public BlobClientOptions? OriginalOptions { get; set; }
}