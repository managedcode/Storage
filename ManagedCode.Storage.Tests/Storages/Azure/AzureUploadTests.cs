using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;

namespace ManagedCode.Storage.Tests.GCP;

public class AzureUploadTests : UploadTests<AzuriteContainer>
{
    protected override AzuriteContainer Build()
    {
        return new AzuriteBuilder().Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return AzureConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}