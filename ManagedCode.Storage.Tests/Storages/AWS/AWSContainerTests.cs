using Microsoft.Extensions.DependencyInjection;
using Testcontainers.LocalStack;

namespace ManagedCode.Storage.Tests.GCP;

public class AWSContainerTests : ContainerTests<LocalStackContainer>
{
    protected override LocalStackContainer Build()
    {
        return new LocalStackBuilder().Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return AWSConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}