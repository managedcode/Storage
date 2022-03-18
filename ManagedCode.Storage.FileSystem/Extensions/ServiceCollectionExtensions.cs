using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.FileSystem.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileSystemStorage(
        this IServiceCollection serviceCollection,
        Action<FSStorageOptions> action)
    {
        var fsStorageOptions = new FSStorageOptions();
        action.Invoke(fsStorageOptions);

        return serviceCollection
            .AddScoped<IFileSystemStorage>(_ => new FileSystemStorage(fsStorageOptions));
    }

    public static IServiceCollection AddFileSystemStorageAsDefault(
        this IServiceCollection serviceCollection,
        Action<FSStorageOptions> action)
    {
        var fsStorageOptions = new FSStorageOptions();
        action.Invoke(fsStorageOptions);

        return serviceCollection
            .AddScoped<IStorage>(_ => new FileSystemStorage(fsStorageOptions));
    }
}