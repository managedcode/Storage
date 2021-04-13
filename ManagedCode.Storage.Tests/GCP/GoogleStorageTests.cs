using System.Threading.Tasks;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Gcp.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.GCP
{
    public class GoogleStorageTests : StorageBaseTests
    {
        public GoogleStorageTests()
        {
            var services = new ServiceCollection();

            services.AddManagedCodeStorage()
                .AddGoogleStorage(opt =>
                {
                    opt.FileName = "google-creds.json";
                })
                    .Add<IDocumentStorage>(opt =>
                    {
                        opt.ProjectId = "api-project-0000000000000";
                        opt.Bucket = "my-docs-1";
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

        [Fact]
        protected async Task WhenGetBlobListAsyncIsCalled()
        {
            await GetBlobListAsyncIsCalled();
        }
    }
}
