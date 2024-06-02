using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public class AzureBlobTests : BlobTests<AzuriteContainer>
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