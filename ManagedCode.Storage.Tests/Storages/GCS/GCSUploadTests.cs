using Microsoft.Extensions.DependencyInjection;
using TestcontainersGCS;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.GCS;


public class GCSUploadTests : UploadTests<GCSContainer>
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