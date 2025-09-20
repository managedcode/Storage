using ManagedCode.Storage.Server.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ManagedCode.Storage.Server.Extensions;

public static class StorageEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapStorageHub(this IEndpointRouteBuilder endpoints, string pattern = "/hubs/storage")
    {
        endpoints.MapHub<StorageHub>(pattern);
        return endpoints;
    }
}
