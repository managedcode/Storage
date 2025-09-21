using ManagedCode.Storage.Server.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ManagedCode.Storage.Server.Extensions;

/// <summary>
/// Provides convenience routing extensions for storage endpoints.
/// </summary>
public static class StorageEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the default storage SignalR hub to the specified route pattern.
    /// </summary>
    /// <param name="endpoints">Endpoint route builder.</param>
    /// <param name="pattern">Route pattern for the hub.</param>
    /// <returns>The original <paramref name="endpoints"/>.</returns>
    public static IEndpointRouteBuilder MapStorageHub(this IEndpointRouteBuilder endpoints, string pattern = "/hubs/storage")
    {
        endpoints.MapHub<StorageHub>(pattern);
        return endpoints;
    }
}
