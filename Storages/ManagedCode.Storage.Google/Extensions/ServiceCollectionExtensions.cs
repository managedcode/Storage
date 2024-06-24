﻿using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.DependencyInjection;

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
        return serviceCollection.AddTransient<IGCPStorage, GCPStorage>();
    }

    public static IServiceCollection AddGCPStorageAsDefault(this IServiceCollection serviceCollection, GCPStorageOptions options)
    {
        CheckConfiguration(options);

        serviceCollection.AddSingleton(options);
        serviceCollection.AddTransient<IGCPStorage, GCPStorage>();
        return serviceCollection.AddTransient<IStorage, GCPStorage>();
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