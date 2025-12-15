using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ManagedCode.Storage.Google.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGCPStorage(this IServiceCollection serviceCollection, Action<GCPStorageOptions> action)
    {
        var options = new GCPStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddGCPStorage(options);
    }

    public static IServiceCollection AddGCPStorageAsDefault(this IServiceCollection serviceCollection, Action<GCPStorageOptions> action)
    {
        var options = new GCPStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddGCPStorageAsDefault(options);
    }

    public static IServiceCollection AddGCPStorage(this IServiceCollection serviceCollection, GCPStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, GCPStorageProvider>();
        return serviceCollection.AddSingleton<IGCPStorage, GCPStorage>();
    }

    public static IServiceCollection AddGCPStorageAsDefault(this IServiceCollection serviceCollection, GCPStorageOptions options)
    {
        CheckConfiguration(options);

        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, GCPStorageProvider>();
        serviceCollection.AddSingleton<IGCPStorage, GCPStorage>();
        return serviceCollection.AddSingleton<IStorage, GCPStorage>();
    }

    public static IServiceCollection AddGCPStorage(this IServiceCollection serviceCollection, string key, Action<GCPStorageOptions> action)
    {
        var options = new GCPStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<GCPStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IGCPStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<GCPStorageOptions>(k);
            return new GCPStorage(opts);
        });

        return serviceCollection;
    }

    public static IServiceCollection AddGCPStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<GCPStorageOptions> action)
    {
        var options = new GCPStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<GCPStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IGCPStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<GCPStorageOptions>(k);
            return new GCPStorage(opts);
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IGCPStorage>(k));

        return serviceCollection;
    }

    private static void CheckConfiguration(GCPStorageOptions options)
    {
        if (options.StorageClientBuilder is null && options.GoogleCredential is null)
            throw new BadConfigurationException($"{nameof(options.StorageClientBuilder)} or {nameof(options.GoogleCredential)} must be assigned");

        if (string.IsNullOrEmpty(options.BucketOptions?.Bucket))
            throw new BadConfigurationException($"{nameof(options.BucketOptions.Bucket)} cannot be empty");

        if (string.IsNullOrEmpty(options.BucketOptions?.ProjectId))
            throw new BadConfigurationException($"{nameof(options.BucketOptions.ProjectId)} cannot be empty");
    }
}