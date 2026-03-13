using System;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Hosting;

/// <summary>
/// <see cref="IServiceCollection"/> extensions for ManagedCode-backed Orleans grain storage.
/// </summary>
public static class ManagedCodeStorageGrainStorageServiceCollectionExtensions
{
    public static IServiceCollection AddGrainStorageAsDefault(
        this IServiceCollection services,
        Action<ManagedCodeStorageGrainStorageOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services.AddGrainStorageAsDefault(options => options.Configure(configureOptions));
    }

    public static IServiceCollection AddGrainStorage(
        this IServiceCollection services,
        string name,
        Action<ManagedCodeStorageGrainStorageOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services.AddGrainStorage(name, options => options.Configure(configureOptions));
    }

    public static IServiceCollection AddGrainStorageAsDefault<TStorage>(
        this IServiceCollection services,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
        where TStorage : class, IStorage
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddGrainStorage<TStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    public static IServiceCollection AddGrainStorage<TStorage>(
        this IServiceCollection services,
        string name,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
        where TStorage : class, IStorage
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);

        return services.AddGrainStorage(name, options =>
        {
            options.Configure(storageOptions => storageOptions.StorageServiceType = typeof(TStorage));
            if (configureOptions is not null)
            {
                options.Configure(configureOptions);
            }
        });
    }

    public static IServiceCollection AddGrainStorageAsDefault(
        this IServiceCollection services,
        string storageKey,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(storageKey);

        return services.AddGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, storageKey, configureOptions);
    }

    public static IServiceCollection AddGrainStorage(
        this IServiceCollection services,
        string name,
        string storageKey,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(storageKey);

        return services.AddGrainStorage(name, options =>
        {
            options.Configure(storageOptions => storageOptions.StorageKey = storageKey);
            if (configureOptions is not null)
            {
                options.Configure(configureOptions);
            }
        });
    }

    public static IServiceCollection AddGrainStorageAsDefault<TStorage>(
        this IServiceCollection services,
        string storageKey,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
        where TStorage : class, IStorage
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(storageKey);

        return services.AddGrainStorage<TStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, storageKey, configureOptions);
    }

    public static IServiceCollection AddGrainStorage<TStorage>(
        this IServiceCollection services,
        string name,
        string storageKey,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
        where TStorage : class, IStorage
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(storageKey);

        return services.AddGrainStorage(name, options =>
        {
            options.Configure(storageOptions =>
            {
                storageOptions.StorageServiceType = typeof(TStorage);
                storageOptions.StorageKey = storageKey;
            });

            if (configureOptions is not null)
            {
                options.Configure(configureOptions);
            }
        });
    }

    public static IServiceCollection AddGrainStorageAsDefault(
        this IServiceCollection services,
        Action<OptionsBuilder<ManagedCodeStorageGrainStorageOptions>>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
    }

    public static IServiceCollection AddGrainStorage(
        this IServiceCollection services,
        string name,
        Action<OptionsBuilder<ManagedCodeStorageGrainStorageOptions>>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);

        configureOptions?.Invoke(services.AddOptions<ManagedCodeStorageGrainStorageOptions>(name));
        services.AddTransient<IConfigurationValidator>(sp =>
            new ManagedCodeStorageGrainStorageOptionsValidator(
                sp.GetRequiredService<IOptionsMonitor<ManagedCodeStorageGrainStorageOptions>>().Get(name),
                name));
        services.AddTransient<IPostConfigureOptions<ManagedCodeStorageGrainStorageOptions>,
            DefaultStorageProviderSerializerOptionsConfigurator<ManagedCodeStorageGrainStorageOptions>>();
        services.ConfigureNamedOptionForLogging<ManagedCodeStorageGrainStorageOptions>(name);
        services.AddKeyedSingleton<IGrainStorage>(name, (sp, key) =>
        {
            var providerName = key as string ?? name;
            var options = sp.GetRequiredService<IOptionsMonitor<ManagedCodeStorageGrainStorageOptions>>().Get(providerName);
            return ManagedCodeGrainStorage.Create(providerName, options, sp);
        });

        if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
        {
            services.TryAddSingleton<IGrainStorage>(sp => sp.GetRequiredKeyedService<IGrainStorage>(name));
        }

        return services;
    }
}
