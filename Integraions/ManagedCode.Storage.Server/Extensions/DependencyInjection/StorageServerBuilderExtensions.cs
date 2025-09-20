using System;
using ManagedCode.Storage.Server.ChunkUpload;
using ManagedCode.Storage.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Server.Extensions.DependencyInjection;

/// <summary>
/// Provides helpers for wiring storage server components into an <see cref="IServiceCollection"/>.
/// </summary>
public static class StorageServerBuilderExtensions
{
    /// <summary>
    /// Registers server-side services required for HTTP controllers and chunk uploads.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureServer">Optional configuration for <see cref="StorageServerOptions"/>.</param>
    /// <param name="configureChunks">Optional configuration for <see cref="ChunkUploadOptions"/>.</param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddStorageServer(this IServiceCollection services, Action<StorageServerOptions>? configureServer = null, Action<ChunkUploadOptions>? configureChunks = null)
    {
        var serverOptions = new StorageServerOptions();
        configureServer?.Invoke(serverOptions);
        services.AddSingleton(serverOptions);

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddChunkUploadHandling(configureChunks);
        return services;
    }
}
