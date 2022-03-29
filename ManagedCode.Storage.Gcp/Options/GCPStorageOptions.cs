using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace ManagedCode.Storage.Gcp.Options;

public class GCPStorageOptions
{
    public string AuthFileName { get; set; } = null!;
    public BucketOptions? BucketOptions { get; set; }
    public GoogleCredential? GoogleCredential { get; set; }
    public CreateBucketOptions? OriginalOptions { get; set; }
    public StorageClientBuilder? StorageClientBuilder { get; set; }
}