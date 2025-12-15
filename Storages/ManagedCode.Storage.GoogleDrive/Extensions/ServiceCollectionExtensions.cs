using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.GoogleDrive.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.GoogleDrive.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGoogleDriveStorage(this IServiceCollection serviceCollection, Action<GoogleDriveStorageOptions> action)
    {
        var options = new GoogleDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddGoogleDriveStorage(options);
    }

    public static IServiceCollection AddGoogleDriveStorageAsDefault(this IServiceCollection serviceCollection, Action<GoogleDriveStorageOptions> action)
    {
        var options = new GoogleDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddGoogleDriveStorageAsDefault(options);
    }

    public static IServiceCollection AddGoogleDriveStorage(this IServiceCollection serviceCollection, GoogleDriveStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, GoogleDriveStorageProvider>();
        return serviceCollection.AddSingleton<IGoogleDriveStorage>(sp => new GoogleDriveStorage(options, sp.GetService<ILogger<GoogleDriveStorage>>()));
    }

    public static IServiceCollection AddGoogleDriveStorageAsDefault(this IServiceCollection serviceCollection, GoogleDriveStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, GoogleDriveStorageProvider>();
        serviceCollection.AddSingleton<IGoogleDriveStorage>(sp => new GoogleDriveStorage(options, sp.GetService<ILogger<GoogleDriveStorage>>()));
        return serviceCollection.AddSingleton<IStorage>(sp => sp.GetRequiredService<IGoogleDriveStorage>());
    }

    public static IServiceCollection AddGoogleDriveStorage(this IServiceCollection serviceCollection, string key, Action<GoogleDriveStorageOptions> action)
    {
        var options = new GoogleDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<GoogleDriveStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IGoogleDriveStorage>(key, (sp, k) =>
        {
            var opts = sp.GetRequiredKeyedService<GoogleDriveStorageOptions>(k);
            return new GoogleDriveStorage(opts, sp.GetService<ILogger<GoogleDriveStorage>>());
        });

        return serviceCollection;
    }

    public static IServiceCollection AddGoogleDriveStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<GoogleDriveStorageOptions> action)
    {
        var options = new GoogleDriveStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<GoogleDriveStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IGoogleDriveStorage>(key, (sp, k) =>
        {
            var opts = sp.GetRequiredKeyedService<GoogleDriveStorageOptions>(k);
            return new GoogleDriveStorage(opts, sp.GetService<ILogger<GoogleDriveStorage>>());
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IGoogleDriveStorage>(k));

        return serviceCollection;
    }

    private static void CheckConfiguration(GoogleDriveStorageOptions options)
    {
        if (options.Client == null && options.DriveService == null)
        {
            throw new BadConfigurationException("Google Drive storage requires either a configured DriveService or a custom IGoogleDriveClient.");
        }
    }
}
