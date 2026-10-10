using Amazon.S3;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using ManagedCode.Storage.Sftp.Extensions;
using ManagedCode.Storage.Sftp.Options;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Registration;

public sealed class ProviderRegistrationValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void AwsRegistration_RejectsMissingBucketOrCredentials(int invalid)
    {
        var options = new AWSStorageOptions { Bucket = "contract", PublicKey = "localkey", SecretKey = "localsecret", OriginalOptions = new AmazonS3Config() };
        if (invalid == 0) options.Bucket = "";
        if (invalid == 1) options.PublicKey = "";
        if (invalid == 2) options.SecretKey = "";
        Should.Throw<BadConfigurationException>(() => new ServiceCollection().AddAWSStorage(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AzureRegistration_RejectsMissingContainerOrConnection(int invalid)
    {
        var options = new AzureStorageOptions { Container = "contract", ConnectionString = "UseDevelopmentStorage=true" };
        if (invalid == 0) options.Container = ""; else options.ConnectionString = "";
        Should.Throw<BadConfigurationException>(() => new ServiceCollection().AddAzureStorage(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GcsRegistration_RejectsMissingClientBucketOrProject(int invalid)
    {
        var options = new GCPStorageOptions
        {
            BucketOptions = new BucketOptions { Bucket = "contract", ProjectId = "contract" },
            StorageClientBuilder = new StorageClientBuilder { UnauthenticatedAccess = true }
        };
        if (invalid == 0) options.StorageClientBuilder = null;
        if (invalid == 1) options.BucketOptions.Bucket = "";
        if (invalid == 2) options.BucketOptions.ProjectId = "";
        Should.Throw<BadConfigurationException>(() => new ServiceCollection().AddGCPStorage(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SftpRegistration_RejectsInvalidConnectionSettings(int invalid)
    {
        var options = new SftpStorageOptions { Host = "127.0.0.1", Port = 22, Username = "test", Password = "test" };
        if (invalid == 0) options.Host = " ";
        if (invalid == 1) options.Port = 0;
        if (invalid == 2) options.Username = "";
        if (invalid == 3) options.Password = null;
        Should.Throw<BadConfigurationException>(() => new ServiceCollection().AddSftpStorage(options));
    }
}
