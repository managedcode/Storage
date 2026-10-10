using System.Threading.Tasks;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.FakeGcsServer;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.GCS;

public class GCSUploadTests : UploadTests<FakeGcsServerContainer>
{
    protected override FakeGcsServerContainer Build()
    {
        return GcsContainerFactory.Create("filesystem");
    }

    protected override ServiceProvider ConfigureServices()
    {
        return GCSConfigurator.ConfigureServices(Container.GetConnectionString());
    }

    [Theory]
    [Trait("Category", "LargeFile")]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public override Task UploadAsync_LargeStream_ShouldRoundTrip(int gigabytes)
    {
        return base.UploadAsync_LargeStream_ShouldRoundTrip(gigabytes);
    }
}
