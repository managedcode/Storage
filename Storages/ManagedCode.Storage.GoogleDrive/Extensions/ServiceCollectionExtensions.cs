using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.GoogleDrive.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.GoogleDrive.Extensions;

/// <summary>
/// Extension methods for registering Google Drive storage in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Google Drive storage to the service collection.
    /// </summary>
    public static IServiceCollection AddGoogleDriveStorage(this IServiceCollection serviceCollection, Action<GoogleDriveStorageOptions> action)
    {
        var options = new GoogleDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddGoogleDriveStorage(options);
    }

    /// <summary>
    /// Adds Google Drive storage as the default IStorage to the service collection.
    /// </summary>
    public static IServiceCollection AddGoogleDriveStorageAsDefault(this IServiceCollection serviceCollection, Action<GoogleDriveStorageOptions> action)
    {
        var options = new GoogleDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddGoogleDriveStorageAsDefault(options);
    }

    /// <summary>
    /// Adds Google Drive storage to the service collection.
    /// </summary>
    public static IServiceCollection AddGoogleDriveStorage(this IServiceCollection serviceCollection, GoogleDriveStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, GoogleDriveStorageProvider>();
        return serviceCollection.AddSingleton<IGoogleDriveStorage, GoogleDriveStorage>();
    }

    /// <summary>
    /// Adds Google Drive storage as the default IStorage to the service collection.
    /// </summary>
    public static IServiceCollection AddGoogleDriveStorageAsDefault(this IServiceCollection serviceCollection, GoogleDriveStorageOptions options)
    {
        CheckConfiguration(options);

        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, GoogleDriveStorageProvider>();
        serviceCollection.AddSingleton<IGoogleDriveStorage, GoogleDriveStorage>();
        return serviceCollection.AddSingleton<IStorage, GoogleDriveStorage>();
    }

    /// <summary>
    /// Adds a keyed Google Drive storage to the service collection.
    /// </summary>
    public static IServiceCollection AddGoogleDriveStorage(this IServiceCollection serviceCollection, string key, Action<GoogleDriveStorageOptions> action)
    {
        var options = new GoogleDriveStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton(key, options);
        serviceCollection.AddKeyedSingleton<IGoogleDriveStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<GoogleDriveStorageOptions>(k);
            return new GoogleDriveStorage(opts!);
        });

        return serviceCollection;
    }

    /// <summary>
    /// Adds a keyed Google Drive storage as the default IStorage to the service collection.
    /// </summary>
    public static IServiceCollection AddGoogleDriveStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<GoogleDriveStorageOptions> action)
    {
        var options = new GoogleDriveStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton(key, options);
        serviceCollection.AddKeyedSingleton<IGoogleDriveStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<GoogleDriveStorageOptions>(k);
            return new GoogleDriveStorage(opts!);
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IGoogleDriveStorage>(k));

        return serviceCollection;
    }

    private static void CheckConfiguration(GoogleDriveStorageOptions options)
    {
        if (string.IsNullOrEmpty(options.FolderId))
        {
            throw new BadConfigurationException($"{nameof(options.FolderId)} is required and cannot be empty");
        }

        var hasCredentials = options.Credential != null ||
                            !string.IsNullOrEmpty(options.ServiceAccountJson) ||
                            !string.IsNullOrEmpty(options.ServiceAccountJsonPath);

        if (!hasCredentials)
        {
            throw new BadConfigurationException(
                $"One of {nameof(options.Credential)}, {nameof(options.ServiceAccountJson)}, or {nameof(options.ServiceAccountJsonPath)} must be assigned");
        }
    }
}


