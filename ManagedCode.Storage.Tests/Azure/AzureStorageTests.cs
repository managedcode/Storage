using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Azure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.IO;
using System.Text;

namespace ManagedCode.Storage.Tests.Azure
{
    public class AzureStorageTests
    {
        private IPhotoStorage _photoStorage;
        private IDocumentStorage _documentStorage;

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

            _photoStorage = provider.GetService<IPhotoStorage>();
            _documentStorage = provider.GetService<IDocumentStorage>();
        }

        [Fact]
        public void WhenDIInitialized()
        {
            _photoStorage.Should().NotBeNull();
            _documentStorage.Should().NotBeNull();
        }

        [Fact]
        public async Task WhenSingleBlobExistsIsCalled()
        {
            var result = await _photoStorage.ExistsAsync("34.png");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task WhenDownloadAsyncIsCalled()
        {
            var stream = await _documentStorage.DownloadAsStreamAsync("a.txt");
            using var sr = new StreamReader(stream, Encoding.UTF8);

            string content = sr.ReadToEnd();

            content.Should().NotBeNull();
        }

        [Fact]
        public async Task WhenDownloadAsyncToFileIsCalled()
        {
            var tempFile = await _documentStorage.DownloadAsync("a.txt");
            using var sr = new StreamReader(tempFile.FileStream, Encoding.UTF8);

            string content = sr.ReadToEnd();

            content.Should().NotBeNull();
        }

        [Fact]
        public async Task WhenUploadAsyncIsCalled()
        {
            var lineToUpload = "some crazy text";

            var byteArray = Encoding.ASCII.GetBytes(lineToUpload);
            var stream = new MemoryStream(byteArray);

            await _documentStorage.UploadAsync("b.txt", stream);
        }

        [Fact]
        public async Task WhenDeleteAsyncIsCalled()
        {
            await _documentStorage.DeleteAsync("a.txt");
        }
    }
}
