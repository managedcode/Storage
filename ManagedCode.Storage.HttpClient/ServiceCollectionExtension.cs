using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace ManagedCode.Storage.HttpClient;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddBlobStorageHttpClient(this IServiceCollection services, string serviceAddress)
    {
        services.AddRefitClient<IBlobStorageApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(serviceAddress);
            });

        services.AddTransient<IBlobStorageClient, BlobStorageClient>();
        return services;
    }
}