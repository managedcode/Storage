using System;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Azure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorage(this IServiceCollection serviceCollection, Action<AzureStorageOptions> action)
    {
        var options = new AzureStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAzureStorage(options);
    }

    public static IServiceCollection AddAzureStorageAsDefault(this IServiceCollection serviceCollection, Action<AzureStorageOptions> action)
    {
        var options = new AzureStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAzureStorageAsDefault(options);
    }

    public static IServiceCollection AddAzureStorage(this IServiceCollection serviceCollection, AzureStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        return serviceCollection.AddScoped<IAzureStorage, AzureStorage>();
    }

    public static IServiceCollection AddAzureStorageAsDefault(this IServiceCollection serviceCollection, AzureStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        return serviceCollection.AddScoped<IStorage, AzureStorage>();
    }

    private static void CheckConfiguration(AzureStorageOptions options)
    {
        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new BadConfigurationException($"{nameof(options.ConnectionString)} cannot be empty");
        }

        if (string.IsNullOrEmpty(options.Container))
        {
            throw new BadConfigurationException($"{nameof(options.Container)} cannot be empty");
        }
    }
}