using System;
using ManagedCode.Storage.AzureDataLake.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.AzureDataLake.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureDataLakeStorage(this IServiceCollection serviceCollection, Action<AzureDataLakeStorageOptions> action)
    {
        var options = new AzureDataLakeStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAzureDataLakeStorage(options);
    }

    public static IServiceCollection AddAzureDataLakeStorageAsDefault(this IServiceCollection serviceCollection, Action<AzureDataLakeStorageOptions> action)
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
        return serviceCollection.AddScoped<IAzureDataLakeStorage, AzureDataLakeStorage>();
    }

    public static IServiceCollection AddAzureDataLakeStorageAsDefault(this IServiceCollection serviceCollection, AzureDataLakeStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        return serviceCollection.AddScoped<IStorage, AzureDataLakeStorage>();
    }

    private static void CheckConfiguration(AzureDataLakeStorageOptions options)
    {
        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new BadConfigurationException($"{nameof(options.ConnectionString)} cannot be empty");
        }

        if (string.IsNullOrEmpty(options.FileSystem))
        {
            throw new BadConfigurationException($"{nameof(options.FileSystem)} cannot be empty");
        }
    }
}