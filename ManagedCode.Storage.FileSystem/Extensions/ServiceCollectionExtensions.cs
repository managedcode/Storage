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
        return serviceCollection.AddScoped<IFileSystemStorage>(_ => new FileSystemStorage(options));
    }

    public static IServiceCollection AddFileSystemStorageAsDefault(this IServiceCollection serviceCollection, FileSystemStorageOptions options)
    {
        return serviceCollection.AddScoped<IStorage>(_ => new FileSystemStorage(options));
    }
}