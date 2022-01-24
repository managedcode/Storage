using System;
using System.IO;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Gcp.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Gcp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGCPStorage(this IServiceCollection serviceCollection, Action<GCPStorageOptions> action)
    {
        var gcpStorageOptions = new GCPStorageOptions();
        action.Invoke(gcpStorageOptions);
        return serviceCollection.AddGCPStorage(gcpStorageOptions);
    }

    public static IServiceCollection AddGCPStorageAsDefault(this IServiceCollection serviceCollection, Action<GCPStorageOptions> action)
    {
        var gcpStorageOptions = new GCPStorageOptions();
        action.Invoke(gcpStorageOptions);
        return serviceCollection.AddGCPStorageAsDefault(gcpStorageOptions);
    }
    
    public static IServiceCollection AddGCPStorage(this IServiceCollection serviceCollection, GCPStorageOptions options)
    {
        return serviceCollection.AddScoped<IGCPStorage>(_ => new GCPStorage(options));
    }

    public static IServiceCollection AddGCPStorageAsDefault(this IServiceCollection serviceCollection, GCPStorageOptions options)
    {
        return serviceCollection.AddScoped<IStorage>(_ => new GCPStorage(options));
    }
}