using Google.Apis.Auth.OAuth2;

namespace ManagedCode.Storage.Gcp.Options;

public class GCPStorageOptions
{
    public string AuthFileName { get; set; }
    public BucketOptions BucketOptions { get; set; }
    internal GoogleCredential GoogleCredential { get; set; }
}