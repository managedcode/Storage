using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.FakeGcsServer;

namespace ManagedCode.Storage.Tests.Storages.GCS;

public class GCSDownloadTests : DownloadTests<FakeGcsServerContainer>
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
}