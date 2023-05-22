using System;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Azure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorage(this IServiceCollection serviceCollection,
        Action<AzureStorageOptions> action)
    {
        var options = new AzureStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAzureStorage(options);
    }

    public static IServiceCollection AddAzureStorageAsDefault(this IServiceCollection serviceCollection,
        Action<AzureStorageOptions> action)
    {
        var options = new AzureStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAzureStorageAsDefault(options);
    }

    public static IServiceCollection AddAzureStorage(this IServiceCollection serviceCollection,
        Action<AzureStorageCredentialsOptions> action)
    {
        var options = new AzureStorageCredentialsOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAzureStorage(options);
    }

    public static IServiceCollection AddAzureStorageAsDefault(this IServiceCollection serviceCollection,
        Action<AzureStorageCredentialsOptions> action)
    {
        var options = new AzureStorageCredentialsOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAzureStorageAsDefault(options);
    }

    public static IServiceCollection AddAzureStorage(this IServiceCollection serviceCollection,
        IAzureStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        return serviceCollection.AddTransient<IAzureStorage, AzureStorage>();
    }

    public static IServiceCollection AddAzureStorageAsDefault(this IServiceCollection serviceCollection,
        IAzureStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddTransient<IAzureStorage, AzureStorage>();
        return serviceCollection.AddTransient<IStorage, AzureStorage>();
    }

    private static void CheckConfiguration(IAzureStorageOptions options)
    {
        if (options is AzureStorageOptions azureStorageOptions)
        {
            if (string.IsNullOrEmpty(azureStorageOptions.ConnectionString))
            {
                throw new BadConfigurationException($"{nameof(azureStorageOptions.ConnectionString)} cannot be empty");
            }

            if (string.IsNullOrEmpty(azureStorageOptions.Container))
            {
                throw new BadConfigurationException($"{nameof(azureStorageOptions.Container)} cannot be empty");
            }
        }

        if (options is AzureStorageCredentialsOptions azureStorageCredentialsOptions)
        {
            if (string.IsNullOrEmpty(azureStorageCredentialsOptions.AccountName))
            {
                throw new BadConfigurationException(
                    $"{nameof(azureStorageCredentialsOptions.AccountName)} cannot be empty");
            }

            if (string.IsNullOrEmpty(azureStorageCredentialsOptions.ContainerName))
            {
                throw new BadConfigurationException(
                    $"{nameof(azureStorageCredentialsOptions.ContainerName)} cannot be empty");
            }

            if (azureStorageCredentialsOptions.Credentials is null)
            {
                throw new BadConfigurationException(
                    $"{nameof(azureStorageCredentialsOptions.Credentials)} cannot be null");
            }
        }
    }
}