using Microsoft.Extensions.DependencyInjection;
using TestcontainersGCS;
using Xunit;

// ReSharper disable MethodHasAsyncOverload

namespace ManagedCode.Storage.Tests.Storages.GCS;


public class GCSBlobTests : BlobTests<GCSContainer>
{
    protected override GCSContainer Build()
    {
        return new GCSBuilder().Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return GCSConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}