using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.OneDrive.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.OneDrive.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOneDriveStorage(this IServiceCollection serviceCollection, Action<OneDriveStorageOptions> action)
    {
        var options = new OneDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddOneDriveStorage(options);
    }

    public static IServiceCollection AddOneDriveStorageAsDefault(this IServiceCollection serviceCollection, Action<OneDriveStorageOptions> action)
    {
        var options = new OneDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddOneDriveStorageAsDefault(options);
    }

    public static IServiceCollection AddOneDriveStorage(this IServiceCollection serviceCollection, OneDriveStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, OneDriveStorageProvider>();
        return serviceCollection.AddSingleton<IOneDriveStorage>(sp => new OneDriveStorage(options, sp.GetService<ILogger<OneDriveStorage>>()));
    }

    public static IServiceCollection AddOneDriveStorageAsDefault(this IServiceCollection serviceCollection, OneDriveStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, OneDriveStorageProvider>();
        serviceCollection.AddSingleton<IOneDriveStorage>(sp => new OneDriveStorage(options, sp.GetService<ILogger<OneDriveStorage>>()));
        return serviceCollection.AddSingleton<IStorage>(sp => sp.GetRequiredService<IOneDriveStorage>());
    }

    public static IServiceCollection AddOneDriveStorage(this IServiceCollection serviceCollection, string key, Action<OneDriveStorageOptions> action)
    {
        var options = new OneDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<OneDriveStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IOneDriveStorage>(key, (sp, k) =>
        {
            var opts = sp.GetRequiredKeyedService<OneDriveStorageOptions>(k);
            return new OneDriveStorage(opts, sp.GetService<ILogger<OneDriveStorage>>());
        });

        return serviceCollection;
    }

    public static IServiceCollection AddOneDriveStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<OneDriveStorageOptions> action)
    {
        var options = new OneDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<OneDriveStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IOneDriveStorage>(key, (sp, k) =>
        {
            var opts = sp.GetRequiredKeyedService<OneDriveStorageOptions>(k);
            return new OneDriveStorage(opts, sp.GetService<ILogger<OneDriveStorage>>());
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IOneDriveStorage>(k));

        return serviceCollection;
    }

    private static void CheckConfiguration(OneDriveStorageOptions options)
    {
        if (options.Client == null && options.GraphClient == null)
        {
            throw new BadConfigurationException("OneDrive storage requires either a configured GraphServiceClient or a custom IOneDriveClient.");
        }

        if (string.IsNullOrWhiteSpace(options.DriveId))
        {
            throw new BadConfigurationException($"{nameof(options.DriveId)} cannot be empty.");
        }
    }
}
