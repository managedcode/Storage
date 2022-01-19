using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Azure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace ManagedCode.Storage.Tests.Azure
{
    public class DependencyInjectionTests
    {
        private IPhotoStorage _photoStorage;
        private IDocumentStorage _documentStorage;

        public DependencyInjectionTests()
        {
            var services = new ServiceCollection();

            services.AddManagedCodeStorage()
                .AddAzureBlobStorage<IPhotoStorage>(opt => {
                    opt.ConnectionString = "DefaultEndpointsProtocol=https;AccountName=storagestudying;AccountKey=4Y4IBrITEoWYMGe0gNju9wvUQrWi//1VvPIDN2dYWccWKy9uuKWnMBXxQlmcy3Q9UIU70ZJiy8ULD9QITxyeTQ==;EndpointSuffix=core.windows.net";
                    opt.Container = "photos";
                })
                .AddAzureBlobStorage<IDocumentStorage>(opt => {
                    opt.ConnectionString = "DefaultEndpointsProtocol=https;AccountName=storagestudying;AccountKey=4Y4IBrITEoWYMGe0gNju9wvUQrWi//1VvPIDN2dYWccWKy9uuKWnMBXxQlmcy3Q9UIU70ZJiy8ULD9QITxyeTQ==;EndpointSuffix=core.windows.net";
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
    }
}
