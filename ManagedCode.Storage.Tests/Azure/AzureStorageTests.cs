using System.Threading.Tasks;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Azure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Azure
{
    public class AzureStorageTests : StorageBaseTests
    {
        public AzureStorageTests()
        {
            var services = new ServiceCollection();

            services.AddManagedCodeStorage()
                .AddAzureBlobStorage(opt =>
                {
                    opt.ConnectionString = "DefaultEndpointsProtocol=https;AccountName=storagestudying;AccountKey=4Y4IBrITEoWYMGe0gNju9wvUQrWi//1VvPIDN2dYWccWKy9uuKWnMBXxQlmcy3Q9UIU70ZJiy8ULD9QITxyeTQ==;EndpointSuffix=core.windows.net";
                })
                    .Add<IPhotoStorage>(opt =>
                    {
                        opt.Container = "photos";
                    })
                    .Add<IDocumentStorage>(opt =>
                    {
                        opt.Container = "documents";
                    });

            var provider = services.BuildServiceProvider();

            _blobStorage = provider.GetService<IDocumentStorage>();
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
    }
}
