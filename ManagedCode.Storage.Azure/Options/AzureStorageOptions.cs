using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ManagedCode.Storage.Azure.Options;

public class AzureStorageOptions
{
    public string ConnectionString { get; set; } = null!;
    public string Container { get; set; } = null!;
    public bool ShouldCreateIfNotExists { get; set; }
    public PublicAccessType PublicAccessType { get; set; }
    public BlobClientOptions? OriginalOptions { get; set; }
}