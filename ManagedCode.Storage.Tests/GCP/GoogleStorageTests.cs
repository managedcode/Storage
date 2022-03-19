using System.Threading.Tasks;
using ManagedCode.Storage.Gcp;
using ManagedCode.Storage.Gcp.Extensions;
using ManagedCode.Storage.Gcp.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.GCP;


public class GoogleStorageTests : StorageBaseTests
{
    public GoogleStorageTests()
    {
        var services = new ServiceCollection();

        services.AddGCPStorage(opt =>
        {
            opt.AuthFileName = "google-creds.json";
            opt.BucketOptions = new BucketOptions()
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket",
            };
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IGCPStorage>();
    }
}
