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

        serviceCollection.AddKeyedSingleton<FileSystemStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IFileSystemStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<FileSystemStorageOptions>(k);
            return new FileSystemStorage(opts);
        });

        return serviceCollection;
    }

    public static IServiceCollection AddFileSystemStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<FileSystemStorageOptions> action)
    {
        var options = new FileSystemStorageOptions();
        action.Invoke(options);

        serviceCollection.AddKeyedSingleton<FileSystemStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IFileSystemStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<FileSystemStorageOptions>(k);
            return new FileSystemStorage(opts);
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IFileSystemStorage>(k));

        return serviceCollection;
    }
}