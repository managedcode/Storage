using System.Threading.Tasks;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.FileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.FileSystem
{
    public class FileSystemTests : StorageBaseTests
    {
        public FileSystemTests()
        {
            var services = new ServiceCollection();

            services.AddManagedCodeStorage()
                .AddFileSystemStorage(opt =>
                {
                    opt.Path = "C:/myfiles";
                })
                    .Add<IPhotoStorage>(opt =>
                    {
                        opt.Path = "photos";
                    })
                    .Add<IDocumentStorage>(opt =>
                    {
                        opt.Path = "documents";
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
