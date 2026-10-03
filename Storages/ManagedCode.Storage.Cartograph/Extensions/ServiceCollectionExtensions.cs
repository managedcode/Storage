using System;
using ManagedCode.Storage.Cartograph.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ManagedCode.Storage.Cartograph.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCartographStorage(this IServiceCollection services, Action<CartographStorageOptions> configure) =>
        services.AddCartographStorage(Configure(configure));

    public static IServiceCollection AddCartographStorage(this IServiceCollection services, CartographStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        services.AddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStorageProvider, CartographStorageProvider>());
        services.AddSingleton<ICartographStorage>(_ => new CartographStorage(options));
        return services;
    }

    public static IServiceCollection AddCartographStorageAsDefault(this IServiceCollection services, Action<CartographStorageOptions> configure) =>
        services.AddCartographStorageAsDefault(Configure(configure));

    public static IServiceCollection AddCartographStorageAsDefault(this IServiceCollection services, CartographStorageOptions options)
    {
        services.AddCartographStorage(options);
        services.AddSingleton<IStorage>(provider => provider.GetRequiredService<ICartographStorage>());
        return services;
    }

    public static IServiceCollection AddCartographStorage(this IServiceCollection services, string key, Action<CartographStorageOptions> configure) =>
        services.AddCartographStorage(key, Configure(configure));

    public static IServiceCollection AddCartographStorage(this IServiceCollection services, string key, CartographStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(options);
        services.TryAddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStorageProvider, CartographStorageProvider>());
        services.AddKeyedSingleton(key, options);
        services.AddKeyedSingleton<ICartographStorage>(key, (_, _) => new CartographStorage(options));
        return services;
    }

    public static IServiceCollection AddCartographStorageAsDefault(this IServiceCollection services, string key,
        Action<CartographStorageOptions> configure) => services.AddCartographStorageAsDefault(key, Configure(configure));

    public static IServiceCollection AddCartographStorageAsDefault(this IServiceCollection services, string key, CartographStorageOptions options)
    {
        services.AddCartographStorage(key, options);
        services.AddKeyedSingleton<IStorage>(key, (provider, serviceKey) => provider.GetRequiredKeyedService<ICartographStorage>(serviceKey));
        return services;
    }

    private static CartographStorageOptions Configure(Action<CartographStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = new CartographStorageOptions();
        configure(options);
        return options;
    }
}
