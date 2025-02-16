using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public class FileSystemContainerTests : ContainerTests<EmptyContainer>
{
    protected override EmptyContainer Build()
    {
        return new EmptyContainer();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return FileSystemConfigurator.ConfigureServices("managed-code-blob");
    }
}