using System;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers;

namespace Orleans.Hosting;

/// <summary>
/// <see cref="ISiloBuilder"/> extensions for ManagedCode-backed Orleans grain storage.
/// </summary>
public static class ManagedCodeStorageGrainStorageSiloBuilderExtensions
{
    public static ISiloBuilder AddGrainStorageAsDefault(
        this ISiloBuilder builder,
        Action<ManagedCodeStorageGrainStorageOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return builder.ConfigureServices(services => services.AddGrainStorageAsDefault(configureOptions));
    }

    public static ISiloBuilder AddGrainStorage(
        this ISiloBuilder builder,
        string name,
        Action<ManagedCodeStorageGrainStorageOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return builder.ConfigureServices(services => services.AddGrainStorage(name, configureOptions));
    }

    public static ISiloBuilder AddGrainStorageAsDefault<TStorage>(
        this ISiloBuilder builder,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
        where TStorage : class, IStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices(services => services.AddGrainStorageAsDefault<TStorage>(configureOptions));
    }

    public static ISiloBuilder AddGrainStorage<TStorage>(
        this ISiloBuilder builder,
        string name,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
        where TStorage : class, IStorage
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        return builder.ConfigureServices(services => services.AddGrainStorage<TStorage>(name, configureOptions));
    }

    public static ISiloBuilder AddGrainStorageAsDefault(
        this ISiloBuilder builder,
        string storageKey,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storageKey);

        return builder.ConfigureServices(services => services.AddGrainStorageAsDefault(storageKey, configureOptions));
    }

    public static ISiloBuilder AddGrainStorage(
        this ISiloBuilder builder,
        string name,
        string storageKey,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(storageKey);

        return builder.ConfigureServices(services => services.AddGrainStorage(name, storageKey, configureOptions));
    }

    public static ISiloBuilder AddGrainStorageAsDefault<TStorage>(
        this ISiloBuilder builder,
        string storageKey,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
        where TStorage : class, IStorage
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storageKey);

        return builder.ConfigureServices(services => services.AddGrainStorageAsDefault<TStorage>(storageKey, configureOptions));
    }

    public static ISiloBuilder AddGrainStorage<TStorage>(
        this ISiloBuilder builder,
        string name,
        string storageKey,
        Action<ManagedCodeStorageGrainStorageOptions>? configureOptions = null)
        where TStorage : class, IStorage
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(storageKey);

        return builder.ConfigureServices(services => services.AddGrainStorage<TStorage>(name, storageKey, configureOptions));
    }

    public static ISiloBuilder AddGrainStorageAsDefault(
        this ISiloBuilder builder,
        Action<OptionsBuilder<ManagedCodeStorageGrainStorageOptions>>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureServices(services => services.AddGrainStorageAsDefault(configureOptions));
    }

    public static ISiloBuilder AddGrainStorage(
        this ISiloBuilder builder,
        string name,
        Action<OptionsBuilder<ManagedCodeStorageGrainStorageOptions>>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        return builder.ConfigureServices(services => services.AddGrainStorage(name, configureOptions));
    }
}
