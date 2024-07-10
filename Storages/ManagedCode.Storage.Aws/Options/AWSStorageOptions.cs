using Amazon.S3;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Aws.Options;

/// <summary>
///     Configuration options for AWS S3 storage.
/// </summary>
public class AWSStorageOptions : IStorageOptions
{
    /// <summary>
    ///     The public key to access the AWS S3 storage bucket.
    /// </summary>
    public string? PublicKey { get; set; }

    /// <summary>
    ///     The secret key to access the AWS S3 storage bucket.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    ///     The name of the IAM role.
    /// </summary>
    /// <remarks>
    ///     If this is set, the <see cref="PublicKey"/> and <see cref="SecretKey"/> will be ignored.
    ///     Note that this can only be used when running on an EC2 instance.
    /// </remarks>
    public string? RoleName { get; set; }

    /// <summary>
    ///     The name of the bucket to use.
    /// </summary>
    public string? Bucket { get; set; }

    /// <summary>
    ///     The underlying Amazon S3 configuration.
    /// </summary>
    public AmazonS3Config? OriginalOptions { get; set; } = new();

    /// <summary>
    ///     Whether to create the container if it does not exist. Default is <c>true</c>.
    /// </summary>
    public bool CreateContainerIfNotExists { get; set; } = true;
    
    /// <summary>
    ///     Whether to use the instance profile credentials. Default is <c>false</c>.
    /// </summary>
    /// <remarks>
    ///    If this is set to <c>true</c>, the <see cref="PublicKey"/> and <see cref="SecretKey"/> will be ignored.
    ///     Note that this can only be used when running on an EC2 instance.
    /// </remarks>
    public bool UseInstanceProfileCredentials { get; set; } = false;
}