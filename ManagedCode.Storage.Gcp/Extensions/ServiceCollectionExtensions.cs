using System;
using System.IO;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using ManagedCode.Storage.Gcp.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Gcp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGCPStorage(
        this IServiceCollection serviceCollection,
        Action<GCPStorageOptions> action)
    {
        var awsStorageOptions = new GCPStorageOptions();
        action.Invoke(awsStorageOptions);

        var path = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            awsStorageOptions.AuthFileName
        );

        using (Stream m = new FileStream(path, FileMode.Open))
        {
            awsStorageOptions.GoogleCredential = GoogleCredential.FromStream(m);
        }


        return serviceCollection
            .AddScoped<IGCPStorage>(_ => new GCPStorage(awsStorageOptions));
    }
}