using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ManagedCode.Storage.Azure.Options;

public class AzureStorageCredentialsOptions : IAzureStorageOptions
{
    public string AccountName { get; set; }
    public string ContainerName { get; set; }

    public TokenCredential Credentials { get; set; }

    public string? Container { get; set; }
    public PublicAccessType PublicAccessType { get; set; }
    public BlobClientOptions? OriginalOptions { get; set; }

    public bool CreateContainerIfNotExists { get; set; } = true;
}