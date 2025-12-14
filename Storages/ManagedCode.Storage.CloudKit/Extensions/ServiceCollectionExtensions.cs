using System;
using ManagedCode.Storage.CloudKit.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.CloudKit.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCloudKitStorage(this IServiceCollection serviceCollection, Action<CloudKitStorageOptions> action)
    {
        var options = new CloudKitStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddCloudKitStorage(options);
    }

    public static IServiceCollection AddCloudKitStorageAsDefault(this IServiceCollection serviceCollection, Action<CloudKitStorageOptions> action)
    {
        var options = new CloudKitStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddCloudKitStorageAsDefault(options);
    }

    public static IServiceCollection AddCloudKitStorage(this IServiceCollection serviceCollection, CloudKitStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, CloudKitStorageProvider>();
        return serviceCollection.AddSingleton<ICloudKitStorage>(sp => new CloudKitStorage(options, sp.GetService<ILogger<CloudKitStorage>>()));
    }

    public static IServiceCollection AddCloudKitStorageAsDefault(this IServiceCollection serviceCollection, CloudKitStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, CloudKitStorageProvider>();
        serviceCollection.AddSingleton<ICloudKitStorage>(sp => new CloudKitStorage(options, sp.GetService<ILogger<CloudKitStorage>>()));
        return serviceCollection.AddSingleton<IStorage>(sp => sp.GetRequiredService<ICloudKitStorage>());
    }

    public static IServiceCollection AddCloudKitStorage(this IServiceCollection serviceCollection, string key, Action<CloudKitStorageOptions> action)
    {
        var options = new CloudKitStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<CloudKitStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<ICloudKitStorage>(key, (sp, k) =>
        {
            var opts = sp.GetRequiredKeyedService<CloudKitStorageOptions>(k);
            return new CloudKitStorage(opts, sp.GetService<ILogger<CloudKitStorage>>());
        });

        return serviceCollection;
    }

    public static IServiceCollection AddCloudKitStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<CloudKitStorageOptions> action)
    {
        var options = new CloudKitStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<CloudKitStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<ICloudKitStorage>(key, (sp, k) =>
        {
            var opts = sp.GetRequiredKeyedService<CloudKitStorageOptions>(k);
            return new CloudKitStorage(opts, sp.GetService<ILogger<CloudKitStorage>>());
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<ICloudKitStorage>(k));

        return serviceCollection;
    }

    private static void CheckConfiguration(CloudKitStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ContainerId))
        {
            throw new BadConfigurationException($"{nameof(options.ContainerId)} cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.RecordType))
        {
            throw new BadConfigurationException($"{nameof(options.RecordType)} cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.PathFieldName) || string.IsNullOrWhiteSpace(options.AssetFieldName))
        {
            throw new BadConfigurationException("CloudKit storage requires configured field names.");
        }

        var hasApiToken = !string.IsNullOrWhiteSpace(options.ApiToken);
        var hasServerKey = !string.IsNullOrWhiteSpace(options.ServerToServerKeyId) || !string.IsNullOrWhiteSpace(options.ServerToServerPrivateKeyPem);

        if (hasApiToken && hasServerKey)
        {
            throw new BadConfigurationException("CloudKit storage must use either API token authentication or server-to-server signing (not both).");
        }

        if (!hasApiToken && options.Client == null)
        {
            if (string.IsNullOrWhiteSpace(options.ServerToServerKeyId) || string.IsNullOrWhiteSpace(options.ServerToServerPrivateKeyPem))
            {
                throw new BadConfigurationException("CloudKit storage requires either an API token (ckAPIToken) or a server-to-server key (key id + private key PEM), unless a custom ICloudKitClient is supplied.");
            }

            if (options.Database != CloudKitDatabase.Public)
            {
                throw new BadConfigurationException("Server-to-server keys are supported only for the public database.");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.WebAuthToken) && !hasApiToken)
        {
            throw new BadConfigurationException($"{nameof(options.WebAuthToken)} requires {nameof(options.ApiToken)}.");
        }
    }
}

