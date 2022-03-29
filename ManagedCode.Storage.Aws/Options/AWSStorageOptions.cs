using Amazon.S3;

namespace ManagedCode.Storage.Aws.Options;

public class AWSStorageOptions
{
    public string? PublicKey { get; set; }
    public string? SecretKey { get; set; }
    public string? Bucket { get; set; }
    public AmazonS3Config? OriginalOptions { get; set; }
}