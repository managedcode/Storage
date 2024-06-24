using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public class FileSystemDownloadTests : DownloadTests<EmptyContainer>
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