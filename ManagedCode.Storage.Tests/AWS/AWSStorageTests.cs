using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using FluentAssertions;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Aws.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.AWS;

public class AWSStorageTests : StorageBaseTests
{
    public AWSStorageTests()
    {
        var services = new ServiceCollection();

        //aws libarary overwrites property values. you should only create configurations this way. 
        var awsConfig = new AmazonS3Config();
        awsConfig.RegionEndpoint = RegionEndpoint.EUWest1;
        awsConfig.ForcePathStyle = true;
        awsConfig.UseHttp = true;
        awsConfig.ServiceURL = "http://localhost:4566"; //this is the default port for the aws s3 emulator, must be last in the list
        
        services.AddAWSStorage(opt =>
        {
            opt.PublicKey = "localkey";
            opt.SecretKey = "localsecret";
            opt.Bucket = "managed-code-bucket";
            opt.OriginalOptions = awsConfig;
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IAWSStorage>();
    }
}