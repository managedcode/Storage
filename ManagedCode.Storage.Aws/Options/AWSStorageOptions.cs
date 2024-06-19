using Amazon.S3;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Aws.Options;

public class AWSStorageOptions : IStorageOptions
{
    public string? PublicKey { get; set; }
    public string? SecretKey { get; set; }
    public string? RoleName { get; set; }
    public string? Bucket { get; set; }
    public AmazonS3Config? OriginalOptions { get; set; } = new();

    public bool CreateContainerIfNotExists { get; set; } = true;
}