using System;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

    public static IServiceCollection AddAzureStorageWithCredential(this IServiceCollection serviceCollection, Action<AzureStorageCredentialsOptions> action)
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

    public static IServiceCollection AddAzureStorage(this IServiceCollection serviceCollection, IAzureStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, AzureStorageProvider>();
        return serviceCollection.AddSingleton<IAzureStorage, AzureStorage>();
    }

    public static IServiceCollection AddAzureStorageAsDefault(this IServiceCollection serviceCollection, IAzureStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, AzureStorageProvider>();
        serviceCollection.AddSingleton<IAzureStorage, AzureStorage>();
        return serviceCollection.AddSingleton<IStorage, AzureStorage>();
    }

    public static IServiceCollection AddAzureStorage(this IServiceCollection serviceCollection, string key, Action<AzureStorageOptions> action)
    {
        var options = new AzureStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);
    
        serviceCollection.AddKeyedSingleton<AzureStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IAzureStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<AzureStorageOptions>(k);
            return new AzureStorage(opts);
        });

        return serviceCollection;
    }

    public static IServiceCollection AddAzureStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<AzureStorageOptions> action)
    {
        var options = new AzureStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);
    
        serviceCollection.AddKeyedSingleton<AzureStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IAzureStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<AzureStorageOptions>(k);
            return new AzureStorage(opts);
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IAzureStorage>(k));

        return serviceCollection;
    }

    private static void CheckConfiguration(IAzureStorageOptions options)
    {
        if (options is AzureStorageOptions azureStorageOptions)
        {
            if (string.IsNullOrEmpty(azureStorageOptions.ConnectionString))
                throw new BadConfigurationException($"{nameof(azureStorageOptions.ConnectionString)} cannot be empty");

            if (string.IsNullOrEmpty(azureStorageOptions.Container))
                throw new BadConfigurationException($"{nameof(azureStorageOptions.Container)} cannot be empty");
        }

        if (options is AzureStorageCredentialsOptions azureStorageCredentialsOptions)
        {
            if (string.IsNullOrEmpty(azureStorageCredentialsOptions.AccountName))
                throw new BadConfigurationException($"{nameof(azureStorageCredentialsOptions.AccountName)} cannot be empty");

            if (string.IsNullOrEmpty(azureStorageCredentialsOptions.ContainerName))
                throw new BadConfigurationException($"{nameof(azureStorageCredentialsOptions.ContainerName)} cannot be empty");

            if (azureStorageCredentialsOptions.Credentials is null)
                throw new BadConfigurationException($"{nameof(azureStorageCredentialsOptions.Credentials)} cannot be null");
        }
    }
}