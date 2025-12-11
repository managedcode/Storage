using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.GoogleDrive;

/// <summary>
/// Upload tests for Google Drive storage using fake implementation.
/// </summary>
public class GoogleDriveUploadTests : UploadTests<EmptyContainer>
{
    protected override EmptyContainer Build()
    {
        return new EmptyContainer();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return GoogleDriveConfigurator.ConfigureServices("googledrive-upload");
    }
}


