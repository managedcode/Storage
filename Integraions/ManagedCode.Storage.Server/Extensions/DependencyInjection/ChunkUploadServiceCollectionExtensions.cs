using System;
using ManagedCode.Storage.Server.ChunkUpload;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Server.Extensions.DependencyInjection;

public static class ChunkUploadServiceCollectionExtensions
{
    public static IServiceCollection AddChunkUploadHandling(this IServiceCollection services, Action<ChunkUploadOptions>? configure = null)
    {
        var options = new ChunkUploadOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ChunkUploadService>();
        return services;
    }
}
