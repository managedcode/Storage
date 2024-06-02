using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Google.Options;

public class GCPStorageOptions : IStorageOptions
{
    public string AuthFileName { get; set; } = null!;
    public BucketOptions BucketOptions { get; set; }
    public GoogleCredential? GoogleCredential { get; set; }
    public CreateBucketOptions? OriginalOptions { get; set; }
    public StorageClientBuilder? StorageClientBuilder { get; set; }

    public bool CreateContainerIfNotExists { get; set; } = true;
}