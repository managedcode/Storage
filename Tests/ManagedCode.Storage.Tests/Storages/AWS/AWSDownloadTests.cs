using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.LocalStack;

namespace ManagedCode.Storage.Tests.Storages.AWS;

public class AWSDownloadTests : DownloadTests<LocalStackContainer>
{
    protected override LocalStackContainer Build()
    {
        return AwsContainerFactory.Create();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return AWSConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}
