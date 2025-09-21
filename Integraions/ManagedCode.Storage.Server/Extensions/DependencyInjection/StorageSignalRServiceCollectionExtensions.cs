using System;
using ManagedCode.Storage.Server.Hubs;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Server.Extensions.DependencyInjection;

/// <summary>
/// Provides registration helpers for SignalR-based storage streaming.
/// </summary>
public static class StorageSignalRServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="StorageHubOptions"/> for SignalR storage hubs.
    /// </summary>
    /// <param name="services">Target service collection.</param>
    /// <param name="configure">Optional configuration delegate for hub options.</param>
    /// <returns>The original <paramref name="services"/>.</returns>
    public static IServiceCollection AddStorageSignalR(this IServiceCollection services, Action<StorageHubOptions>? configure = null)
    {
        var options = new StorageHubOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        return services;
    }
}
