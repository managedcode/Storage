using System;
using System.IO;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.GoogleDrive;
using ManagedCode.Storage.TestFakes;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.GoogleDrive;

/// <summary>
/// Configurator for Google Drive storage tests.
/// Uses FakeGoogleDriveStorage which is backed by FileSystemStorage for testing.
/// </summary>
public class GoogleDriveConfigurator
{
    public static ServiceProvider ConfigureServices(string basePath)
    {
        basePath += Random.Shared.NextInt64();
        var services = new ServiceCollection();
        var fullPath = Path.Combine(Environment.CurrentDirectory, basePath);

        // Use fake storage backed by file system for tests
        var fakeStorage = new FakeGoogleDriveStorage(fullPath);

        services.AddSingleton<IGoogleDriveStorage>(fakeStorage);
        services.AddSingleton<IStorage>(fakeStorage);

        return services.BuildServiceProvider();
    }
}


