using Microsoft.Extensions.DependencyInjection;
using Testcontainers.GCS;

// ReSharper disable MethodHasAsyncOverload

namespace ManagedCode.Storage.Tests.GCP;

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