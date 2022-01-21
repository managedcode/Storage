using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Aws.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.AWS
{
    public class AWSStorageTests : StorageBaseTests
    {
        public AWSStorageTests()
        {
            var services = new ServiceCollection();

            services.AddManagedCodeStorage()
                .AddAWSStorage(opt =>
                {
                    opt.PublicKey = "AKIAS7SSNCWKJLPBKNHX";
                    opt.SecretKey = "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";
                })
                    .Add<IDocumentStorage>(opt =>
                    {
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
            var stream = await _blobStorage.DownloadAsStreamAsync("a.txt");
            using var sr = new StreamReader(stream, Encoding.UTF8);

            string content = sr.ReadToEnd();

            content.Should().NotBeNull();
        }

        [Fact]
        public async Task WhenDownloadAsyncToFileIsCalled()
        {
            await DownloadAsyncToFileIsCalled("a.txt");
        }

        [Fact]
        public async Task WhenUploadAsyncIsCalled()
        {
            await UploadAsyncIsCalled("d.txt");
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
