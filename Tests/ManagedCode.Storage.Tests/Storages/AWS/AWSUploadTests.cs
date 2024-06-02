using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.LocalStack;

namespace ManagedCode.Storage.Tests.Storages.AWS;

public class AWSUploadTests : UploadTests<LocalStackContainer>
{
    protected override LocalStackContainer Build()
    {
        return new LocalStackBuilder().WithImage(ContainerImages.LocalStack)
            .Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return AWSConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}