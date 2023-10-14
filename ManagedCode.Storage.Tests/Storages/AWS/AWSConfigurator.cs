using System.ComponentModel;
using Amazon.S3;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.GCS;

// ReSharper disable MethodHasAsyncOverload

namespace ManagedCode.Storage.Tests.GCP;

public class AWSConfigurator
{
    public static ServiceProvider ConfigureServices(string connectionString)
    {

        var services = new ServiceCollection();
        
        var config = new AmazonS3Config();
        config.ServiceURL = connectionString;
        
        services.AddAWSStorageAsDefault(opt =>
        {
            opt.PublicKey = "localkey";
            opt.SecretKey = "localsecret";
            opt.Bucket = "managed-code-bucket";
            opt.OriginalOptions = config;
        });

        services.AddAWSStorage(new AWSStorageOptions
        {
            PublicKey = "localkey",
            SecretKey = "localsecret",
            Bucket = "managed-code-bucket",
            OriginalOptions = config
        });
        return services.BuildServiceProvider();
    }
}