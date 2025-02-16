using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public class AzureDownloadTests : DownloadTests<AzuriteContainer>
{
    protected override AzuriteContainer Build()
    {
        return new AzuriteBuilder().WithImage(ContainerImages.Azurite)
            .Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return AzureConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}