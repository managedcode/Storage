using Microsoft.Extensions.DependencyInjection;
using TestcontainersGCS;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.GCS;

[Collection("Google")]
public class GCSDownloadTests : DownloadTests<GCSContainer>
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