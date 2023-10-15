using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.GCS;

[Collection("Google")]
public class GCSConfigurator
{
    public static ServiceProvider ConfigureServices(string connectionString)
    {

        var services = new ServiceCollection();

        services.AddGCPStorageAsDefault(opt =>
        {
            opt.BucketOptions = new BucketOptions
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket"
            };
            opt.StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = connectionString
            };
        });

        services.AddGCPStorage(new GCPStorageOptions
        {
            BucketOptions = new BucketOptions
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket"
            },
            StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = connectionString
            }
        });
        return services.BuildServiceProvider();
    }
}