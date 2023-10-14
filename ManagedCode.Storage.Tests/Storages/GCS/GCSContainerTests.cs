using Microsoft.Extensions.DependencyInjection;
using Testcontainers.GCS;

namespace ManagedCode.Storage.Tests.Storages.GCS;

public class GCSContainerTests : ContainerTests<GCSContainer>
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