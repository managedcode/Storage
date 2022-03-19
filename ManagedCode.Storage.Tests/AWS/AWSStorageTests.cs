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
/*
public class AWSStorageTests : StorageBaseTests
{
    public AWSStorageTests()
    {
        var services = new ServiceCollection();

        services.AddAWSStorage(opt =>
        {
            opt.PublicKey = "localkey";
            opt.SecretKey = "localsecret";
            opt.Bucket = "managed-code-bucket";
            opt.OriginalOptions = new AmazonS3Config
            {
                ServiceURL = "http://localhost:4566",
                RegionEndpoint = RegionEndpoint.EUWest1,
                ForcePathStyle = true,
                UseHttp = true,
            };
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IAWSStorage>();
    }
}
*/