using System;
using ManagedCode.Storage.Browser.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace ManagedCode.Storage.Browser.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBrowserStorage(this IServiceCollection services, Action<BrowserStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BrowserStorageOptions();
        configure(options);
        return services.AddBrowserStorage(options);
    }

    public static IServiceCollection AddBrowserStorageAsDefault(this IServiceCollection services, Action<BrowserStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BrowserStorageOptions();
        configure(options);
        return services.AddBrowserStorageAsDefault(options);
    }

    public static IServiceCollection AddBrowserStorage(this IServiceCollection services, BrowserStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        services.AddSingleton(options.Clone());
        services.AddSingleton<IStorageProvider, BrowserStorageProvider>();
        services.AddScoped<IBrowserStorage>(sp => new BrowserStorage(options.WithJsRuntime(sp.GetRequiredService<IJSRuntime>())));
        return services;
    }

    public static IServiceCollection AddBrowserStorageAsDefault(this IServiceCollection services, BrowserStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        services.AddSingleton(options.Clone());
        services.AddSingleton<IStorageProvider, BrowserStorageProvider>();
        services.AddScoped<IBrowserStorage>(sp => new BrowserStorage(options.WithJsRuntime(sp.GetRequiredService<IJSRuntime>())));
        services.AddScoped<IStorage>(sp => sp.GetRequiredService<IBrowserStorage>());
        return services;
    }

    public static IServiceCollection AddBrowserStorage(this IServiceCollection services, string key, Action<BrowserStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BrowserStorageOptions();
        configure(options);

        ValidateOptions(options);

        services.AddKeyedSingleton<BrowserStorageOptions>(key, options.Clone());
        services.AddKeyedScoped<IBrowserStorage>(key, (sp, serviceKey) =>
        {
            var keyedOptions = sp.GetRequiredKeyedService<BrowserStorageOptions>(serviceKey!);
            return new BrowserStorage(keyedOptions.WithJsRuntime(sp.GetRequiredService<IJSRuntime>()));
        });

        return services;
    }

    public static IServiceCollection AddBrowserStorageAsDefault(this IServiceCollection services, string key, Action<BrowserStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new BrowserStorageOptions();
        configure(options);

        ValidateOptions(options);

        services.AddKeyedSingleton<BrowserStorageOptions>(key, options.Clone());
        services.AddKeyedScoped<IBrowserStorage>(key, (sp, serviceKey) =>
        {
            var keyedOptions = sp.GetRequiredKeyedService<BrowserStorageOptions>(serviceKey!);
            return new BrowserStorage(keyedOptions.WithJsRuntime(sp.GetRequiredService<IJSRuntime>()));
        });
        services.AddKeyedScoped<IStorage>(key, (sp, serviceKey) => sp.GetRequiredKeyedService<IBrowserStorage>(serviceKey!));

        return services;
    }

    private static void ValidateOptions(BrowserStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ContainerName))
            throw new BadConfigurationException("Browser storage requires a non-empty container name.");

        if (string.IsNullOrWhiteSpace(options.DatabaseName))
            throw new BadConfigurationException("Browser storage requires a non-empty database name.");

        if (options.ChunkSizeBytes <= 0)
            throw new BadConfigurationException("Browser storage requires ChunkSizeBytes to be greater than zero.");

        if (options.ChunkBatchSize <= 0)
            throw new BadConfigurationException("Browser storage requires ChunkBatchSize to be greater than zero.");
    }

    private static BrowserStorageOptions Clone(this BrowserStorageOptions options)
    {
        return new BrowserStorageOptions
        {
            ContainerName = options.ContainerName,
            DatabaseName = options.DatabaseName,
            ChunkSizeBytes = options.ChunkSizeBytes,
            ChunkBatchSize = options.ChunkBatchSize,
            CreateContainerIfNotExists = options.CreateContainerIfNotExists,
            JsRuntime = options.JsRuntime
        };
    }

    private static BrowserStorageOptions WithJsRuntime(this BrowserStorageOptions options, IJSRuntime jsRuntime)
    {
        var clone = options.Clone();
        clone.JsRuntime = jsRuntime;
        return clone;
    }
}
