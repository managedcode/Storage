using FluentAssertions;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.FileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ManagedCode.Storage.Tests.FileSystem
{
    public class FileSystemTests
    {
        private IPhotoStorage _photoStorage;
        private IDocumentStorage _documentStorage;

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
            var result = await _documentStorage.ExistsAsync("a.txt");

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
    }
}
