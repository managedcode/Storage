using Amazon.S3;

namespace ManagedCode.Storage.Aws.Options;

public class AWSStorageOptions
{
    public string PublicKey { get; set; } = null!;
    public string SecretKey { get; set; } = null!;
    public string Bucket { get; set; } = null!;
    public AmazonS3Config? OriginalOptions { get; set; }
}