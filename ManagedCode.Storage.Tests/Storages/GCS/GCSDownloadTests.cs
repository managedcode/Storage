using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.GCS;

namespace ManagedCode.Storage.Tests.GCP;

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