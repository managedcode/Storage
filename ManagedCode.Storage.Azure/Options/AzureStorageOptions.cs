using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ManagedCode.Storage.Azure.Options;

public class AzureStorageOptions
{
    public string? ConnectionString { get; set; }
    public string? Container { get; set; }
    public bool ShouldCreateIfNotExists { get; set; } = true;
    public PublicAccessType PublicAccessType { get; set; }
    public BlobClientOptions? OriginalOptions { get; set; }
}