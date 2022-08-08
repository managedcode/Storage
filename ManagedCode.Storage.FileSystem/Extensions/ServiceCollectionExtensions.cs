using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.FileSystem.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileSystemStorage(this IServiceCollection serviceCollection, Action<FileSystemStorageOptions> action)
    {
        var fsStorageOptions = new FileSystemStorageOptions();
        action.Invoke(fsStorageOptions);
        return serviceCollection.AddFileSystemStorage(fsStorageOptions);
    }

    public static IServiceCollection AddFileSystemStorageAsDefault(this IServiceCollection serviceCollection, Action<FileSystemStorageOptions> action)
    {
        var fsStorageOptions = new FileSystemStorageOptions();
        action.Invoke(fsStorageOptions);

        return serviceCollection.AddFileSystemStorageAsDefault(fsStorageOptions);
    }

    public static IServiceCollection AddFileSystemStorage(this IServiceCollection serviceCollection, FileSystemStorageOptions options)
    {
        serviceCollection.AddSingleton(options);
        return serviceCollection.AddScoped<IFileSystemStorage, FileSystemStorage>();
    }

    public static IServiceCollection AddFileSystemStorageAsDefault(this IServiceCollection serviceCollection, FileSystemStorageOptions options)
    {
        serviceCollection.AddSingleton(options);
        return serviceCollection.AddScoped<IStorage, FileSystemStorage>();
    }
}