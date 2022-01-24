using System;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Azure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorage(this IServiceCollection serviceCollection, Action<AzureStorageOptions> action)
    {
        var azureStorageOptions = new AzureStorageOptions();
        action.Invoke(azureStorageOptions);
        return serviceCollection.AddAzureStorage(azureStorageOptions);
    }

    public static IServiceCollection AddAzureStorageAsDefault(this IServiceCollection serviceCollection, Action<AzureStorageOptions> action)
    {
        var azureStorageOptions = new AzureStorageOptions();
        action.Invoke(azureStorageOptions);
        return serviceCollection.AddAzureStorageAsDefault(azureStorageOptions);
    }
    
    public static IServiceCollection AddAzureStorage(this IServiceCollection serviceCollection, AzureStorageOptions options)
    {
        return serviceCollection.AddScoped<IAzureStorage>(_ => new AzureStorage(options));
    }

    public static IServiceCollection AddAzureStorageAsDefault(this IServiceCollection serviceCollection, AzureStorageOptions options)
    {
        return serviceCollection.AddScoped<IStorage>(_ => new AzureStorage(options));
    }
}