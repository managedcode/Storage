using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        serviceCollection.AddSingleton<IStorageProvider, FileSystemStorageProvider>();
        return serviceCollection.AddSingleton<IFileSystemStorage>(sp => new FileSystemStorage(options));
    }

    public static IServiceCollection AddFileSystemStorageAsDefault(this IServiceCollection serviceCollection, FileSystemStorageOptions options)
    {
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, FileSystemStorageProvider>();
        serviceCollection.AddSingleton<IFileSystemStorage>(sp => new FileSystemStorage(options));
        return serviceCollection.AddSingleton<IStorage>(sp => new FileSystemStorage(options));
    }

    public static IServiceCollection AddFileSystemStorage(this IServiceCollection serviceCollection, string key, Action<FileSystemStorageOptions> action)
    {
        var options = new FileSystemStorageOptions();
        action.Invoke(options);
        
        serviceCollection.AddKeyedSingleton<FileSystemStorageOptions>(key, (_, _) => options);
        return serviceCollection.AddKeyedSingleton<IFileSystemStorage, FileSystemStorage>(key);
    }

    public static IServiceCollection AddFileSystemStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<FileSystemStorageOptions> action)
    {
        var options = new FileSystemStorageOptions();
        action.Invoke(options);
        
        serviceCollection.AddKeyedSingleton<FileSystemStorageOptions>(key, (_, _) => options);
        serviceCollection.AddKeyedScoped<IFileSystemStorage, FileSystemStorage>(key);
        return serviceCollection.AddKeyedScoped<IStorage, FileSystemStorage>(key);
    }
}