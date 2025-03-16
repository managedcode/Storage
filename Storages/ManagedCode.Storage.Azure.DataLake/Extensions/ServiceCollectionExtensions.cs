using System;
using ManagedCode.Storage.Azure.DataLake.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ManagedCode.Storage.Azure.DataLake.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureDataLakeStorage(this IServiceCollection serviceCollection, Action<AzureDataLakeStorageOptions> action)
    {
        var options = new AzureDataLakeStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAzureDataLakeStorage(options);
    }

    public static IServiceCollection AddAzureDataLakeStorageAsDefault(this IServiceCollection serviceCollection,
        Action<AzureDataLakeStorageOptions> action)
    {
        var options = new AzureDataLakeStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAzureDataLakeStorageAsDefault(options);
    }

    public static IServiceCollection AddAzureDataLakeStorage(this IServiceCollection serviceCollection, AzureDataLakeStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, AzureDataLakeStorageProvider>();
        return serviceCollection.AddSingleton<IAzureDataLakeStorage, AzureDataLakeStorage>();
    }

    public static IServiceCollection AddAzureDataLakeStorageAsDefault(this IServiceCollection serviceCollection, AzureDataLakeStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, AzureDataLakeStorageProvider>();
        serviceCollection.AddSingleton<IAzureDataLakeStorage, AzureDataLakeStorage>();
        return serviceCollection.AddSingleton<IStorage, AzureDataLakeStorage>();
    }
    
    public static IServiceCollection AddAzureDataLakeStorage(this IServiceCollection serviceCollection, string key, Action<AzureDataLakeStorageOptions> action)
    {
        var options = new AzureDataLakeStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);
    
        serviceCollection.AddKeyedSingleton<AzureDataLakeStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IAzureDataLakeStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<AzureDataLakeStorageOptions>(k);
            return new AzureDataLakeStorage(opts);
        });

        return serviceCollection;
    }

    public static IServiceCollection AddAzureDataLakeStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<AzureDataLakeStorageOptions> action)
    {
        var options = new AzureDataLakeStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);
    
        serviceCollection.AddKeyedSingleton<AzureDataLakeStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IAzureDataLakeStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<AzureDataLakeStorageOptions>(k);
            return new AzureDataLakeStorage(opts);
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IAzureDataLakeStorage>(k));

        return serviceCollection;
    }

    private static void CheckConfiguration(AzureDataLakeStorageOptions options)
    {
        if (string.IsNullOrEmpty(options.ConnectionString))
            throw new BadConfigurationException($"{nameof(options.ConnectionString)} cannot be empty");

        if (string.IsNullOrEmpty(options.FileSystem))
            throw new BadConfigurationException($"{nameof(options.FileSystem)} cannot be empty");
    }
}