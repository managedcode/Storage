using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Dropbox.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Dropbox.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDropboxStorage(this IServiceCollection serviceCollection, Action<DropboxStorageOptions> action)
    {
        var options = new DropboxStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddDropboxStorage(options);
    }

    public static IServiceCollection AddDropboxStorageAsDefault(this IServiceCollection serviceCollection, Action<DropboxStorageOptions> action)
    {
        var options = new DropboxStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddDropboxStorageAsDefault(options);
    }

    public static IServiceCollection AddDropboxStorage(this IServiceCollection serviceCollection, DropboxStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, DropboxStorageProvider>();
        return serviceCollection.AddSingleton<IDropboxStorage>(sp => new DropboxStorage(options, sp.GetService<ILogger<DropboxStorage>>()));
    }

    public static IServiceCollection AddDropboxStorageAsDefault(this IServiceCollection serviceCollection, DropboxStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, DropboxStorageProvider>();
        serviceCollection.AddSingleton<IDropboxStorage>(sp => new DropboxStorage(options, sp.GetService<ILogger<DropboxStorage>>()));
        return serviceCollection.AddSingleton<IStorage>(sp => sp.GetRequiredService<IDropboxStorage>());
    }

    public static IServiceCollection AddDropboxStorage(this IServiceCollection serviceCollection, string key, Action<DropboxStorageOptions> action)
    {
        var options = new DropboxStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<DropboxStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IDropboxStorage>(key, (sp, k) =>
        {
            var opts = sp.GetRequiredKeyedService<DropboxStorageOptions>(k);
            return new DropboxStorage(opts, sp.GetService<ILogger<DropboxStorage>>());
        });

        return serviceCollection;
    }

    public static IServiceCollection AddDropboxStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<DropboxStorageOptions> action)
    {
        var options = new DropboxStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<DropboxStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IDropboxStorage>(key, (sp, k) =>
        {
            var opts = sp.GetRequiredKeyedService<DropboxStorageOptions>(k);
            return new DropboxStorage(opts, sp.GetService<ILogger<DropboxStorage>>());
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IDropboxStorage>(k));

        return serviceCollection;
    }

    private static void CheckConfiguration(DropboxStorageOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.RefreshToken) && string.IsNullOrWhiteSpace(options.AppKey))
        {
            throw new BadConfigurationException("Dropbox storage configuration with a refresh token requires AppKey (and optionally AppSecret).");
        }

        if (options.Client == null
            && options.DropboxClient == null
            && string.IsNullOrWhiteSpace(options.AccessToken)
            && string.IsNullOrWhiteSpace(options.RefreshToken))
        {
            throw new BadConfigurationException("Dropbox storage requires either a configured DropboxClient, a custom IDropboxClientWrapper, or credentials (AccessToken / RefreshToken + AppKey).");
        }
    }
}
