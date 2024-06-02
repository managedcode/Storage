using System;
using ManagedCode.Storage.Azure.DataLake.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

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
        return serviceCollection.AddTransient<IAzureDataLakeStorage, AzureDataLakeStorage>();
    }

    public static IServiceCollection AddAzureDataLakeStorageAsDefault(this IServiceCollection serviceCollection, AzureDataLakeStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddTransient<IAzureDataLakeStorage, AzureDataLakeStorage>();
        return serviceCollection.AddTransient<IStorage, AzureDataLakeStorage>();
    }

    private static void CheckConfiguration(AzureDataLakeStorageOptions options)
    {
        if (string.IsNullOrEmpty(options.ConnectionString))
            throw new BadConfigurationException($"{nameof(options.ConnectionString)} cannot be empty");

        if (string.IsNullOrEmpty(options.FileSystem))
            throw new BadConfigurationException($"{nameof(options.FileSystem)} cannot be empty");
    }
}