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
        return new FakeGcsServerBuilder().WithImage(ContainerImages.FakeGCSServer)
            .Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return GCSConfigurator.ConfigureServices(Container.GetConnectionString());
    }

    [Theory(Skip = "FakeGcsServer currently throttles uploads beyond ~10MB; skip large-stream scenario for emulator")]
    [Trait("Category", "LargeFile")]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public override Task UploadAsync_LargeStream_ShouldRoundTrip(int gigabytes)
    {
        _ = gigabytes;
        return Task.CompletedTask;
    }
}
