using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MethodHasAsyncOverload

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public class FileSystemBlobTests : BlobTests<EmptyContainer>
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