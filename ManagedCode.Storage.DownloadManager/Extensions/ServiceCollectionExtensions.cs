using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.DownloadManager.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDownloadManager(
        this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddScoped<IDownloadManager, DownloadManager>();
    }

    public static IServiceCollection AddDownloadManager(
        this IServiceCollection serviceCollection,
        IStorage storage)
    {
        return serviceCollection
            .AddScoped<IDownloadManager>(_ => new DownloadManager(storage));
    }
}