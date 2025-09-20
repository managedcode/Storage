using System;
using ManagedCode.Storage.Server.ChunkUpload;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Server.Extensions.DependencyInjection;

/// <summary>
/// Provides DI helpers for configuring chunk upload services.
/// </summary>
public static class ChunkUploadServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ChunkUploadService"/> with optional configuration.
    /// </summary>
    public static IServiceCollection AddChunkUploadHandling(this IServiceCollection services, Action<ChunkUploadOptions>? configure = null)
    {
        var options = new ChunkUploadOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ChunkUploadService>();
        return services;
    }
}
