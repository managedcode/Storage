using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Azure.Options;

public interface IAzureStorageOptions : IStorageOptions
{
    public string? Container { get; set; }
    public PublicAccessType PublicAccessType { get; set; }
    public BlobClientOptions? OriginalOptions { get; set; }
    public StorageTransferOptions? UploadTransferOptions { get; set; }
}
