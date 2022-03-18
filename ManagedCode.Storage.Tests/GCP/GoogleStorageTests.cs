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
                Bucket = "my-docs-1",
            };
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IGCPStorage>();
    }

    [Fact]
    public void WhenDIInitialized()
    {
        DIInitialized();
    }

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        await SingleBlobExistsIsCalled("a.txt");
    }

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        await DownloadAsyncIsCalled("a.txt");
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        await DownloadAsyncToFileIsCalled("a.txt");
    }

    [Fact]
    public async Task WhenUploadAsyncIsCalled()
    {
        await UploadAsyncIsCalled("a.txt");
    }

    [Fact]
    public async Task WhenDeleteAsyncIsCalled()
    {
        await DeleteAsyncIsCalled("a.txt");
    }

    [Fact]
    protected async Task WhenGetBlobListAsyncIsCalled()
    {
        await GetBlobListAsyncIsCalled();
    }
}
