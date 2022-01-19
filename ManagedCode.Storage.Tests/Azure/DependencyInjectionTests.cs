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
           
        }

        [Fact]
        public void WhenDIInitialized()
        {
            var services = new ServiceCollection();

            services.AddManagedCodeStorage()
                .AddAzureBlobStorage<IPhotoStorage>(opt => {
                    opt.ConnectionString = "";
                    opt.Container = "photos";
                })
                .AddAzureBlobStorage<IDocumentStorage>(opt => {
                    opt.ConnectionString = "";
                    opt.Container = "documents";
                });

            var provider = services.BuildServiceProvider();

            _photoStorage = provider.GetService<IPhotoStorage>();
            _documentStorage = provider.GetService<IDocumentStorage>();

            _photoStorage.Should().NotBeNull();
            _documentStorage.Should().NotBeNull();
        }
    }
}
