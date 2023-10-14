using Microsoft.Extensions.DependencyInjection;
using Testcontainers.GCS;

// ReSharper disable MethodHasAsyncOverload

namespace ManagedCode.Storage.Tests.GCP;

public class FileSystemBlobTests : BlobTests<EmptyContainer>
{
    protected override EmptyContainer Build()
    {
        return new EmptyContainer();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return GCSConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}

public class FileSystemDownloadTests : DownloadTests<EmptyContainer>
{
    protected override EmptyContainer Build()
    {
        return new EmptyContainer();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return GCSConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}

public class FileSystemUploadTests : UploadTests<EmptyContainer>
{
    protected override EmptyContainer Build()
    {
        return new EmptyContainer();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return GCSConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}

public class FileSystemContainerTests : ContainerTests<EmptyContainer>
{
    protected override EmptyContainer Build()
    {
        return new EmptyContainer();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return GCSConfigurator.ConfigureServices(Container.GetConnectionString());
    }
}