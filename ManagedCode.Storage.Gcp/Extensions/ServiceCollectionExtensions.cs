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
    public static IServiceCollection AddGCPStorage(
        this IServiceCollection serviceCollection,
        Action<GCPStorageOptions> action)
    {
        var gcpStorageOptions = new GCPStorageOptions();
        action.Invoke(gcpStorageOptions);

        var path = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            gcpStorageOptions.AuthFileName
        );

        using (Stream m = new FileStream(path, FileMode.Open))
        {
            gcpStorageOptions.GoogleCredential = GoogleCredential.FromStream(m);
        }

        return serviceCollection
            .AddScoped<IGCPStorage>(_ => new GCPStorage(gcpStorageOptions));
    }

    public static IServiceCollection AddGCPStorageAsDefault(
        this IServiceCollection serviceCollection,
        Action<GCPStorageOptions> action)
    {
        var gcpStorageOptions = new GCPStorageOptions();
        action.Invoke(gcpStorageOptions);

        var path = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            gcpStorageOptions.AuthFileName
        );

        using (Stream m = new FileStream(path, FileMode.Open))
        {
            gcpStorageOptions.GoogleCredential = GoogleCredential.FromStream(m);
        }

        return serviceCollection
            .AddScoped<IStorage>(_ => new GCPStorage(gcpStorageOptions));
    }
}