using System;
using System.IO;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public class FileSystemConfigurator
{
    public static ServiceProvider ConfigureServices(string connectionString)
    {
        connectionString += Random.Shared.NextInt64();
        var services = new ServiceCollection();
        
        services.AddFileSystemStorageAsDefault(opt => { opt.BaseFolder = Path.Combine(Environment.CurrentDirectory,connectionString); });
        services.AddFileSystemStorage(new FileSystemStorageOptions
        {
            BaseFolder = Path.Combine(Environment.CurrentDirectory, connectionString)
        });
        return services.BuildServiceProvider();
    }
}