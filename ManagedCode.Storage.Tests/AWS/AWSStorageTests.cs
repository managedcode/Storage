using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using FluentAssertions;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.AWS;

public class AWSStorageTests : StorageBaseTests
{
    protected override ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        //aws libarary overwrites property values. you should only create configurations this way. 
        var awsConfig = new AmazonS3Config();
        awsConfig.RegionEndpoint = RegionEndpoint.EUWest1;
        awsConfig.ForcePathStyle = true;
        awsConfig.UseHttp = true;
        awsConfig.ServiceURL = "http://localhost:4566"; //this is the default port for the aws s3 emulator, must be last in the list

        services.AddAWSStorageAsDefault(opt =>
        {
            opt.PublicKey = "localkey";
            opt.SecretKey = "localsecret";
            opt.Bucket = "managed-code-bucket";
            opt.OriginalOptions = awsConfig;
        });

        services.AddAWSStorage(new AWSStorageOptions
        {
            PublicKey = "localkey",
            SecretKey = "localsecret",
            Bucket = "managed-code-bucket",
            OriginalOptions = awsConfig
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public void StorageAsDefaultTest()
    {
        var storage = ServiceProvider.GetService<IAWSStorage>();
        var defaultStorage = ServiceProvider.GetService<IStorage>();
        storage?.GetType().FullName.Should().Be(defaultStorage?.GetType().FullName);
    }
}