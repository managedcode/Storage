using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.AzureDataLake;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Gcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ManagedCode.Storage.TestFakes;

public static class MockCollectionExtensions
{
    public static IServiceCollection ReplaceAWSStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceAWSStorage();
        serviceCollection.AddScoped<IStorage, FakeAWSStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAWSStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IAWSStorage>();
        serviceCollection.RemoveAll<AWSStorage>();
        serviceCollection.AddScoped<IAWSStorage, FakeAWSStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureDataLakeStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IAzureDataLakeStorage>();
        serviceCollection.RemoveAll<AzureDataLakeStorage>();
        serviceCollection.AddScoped<IAzureDataLakeStorage, FakeAzureDataLakeStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureDataLakeStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceAzureDataLakeStorage();
        serviceCollection.AddScoped<IStorage, FakeAzureDataLakeStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IAzureStorage>();
        serviceCollection.RemoveAll<FakeAzureStorage>();
        serviceCollection.AddScoped<IAzureStorage, FakeAzureStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceAzureStorage();
        serviceCollection.AddScoped<IStorage, FakeAzureStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceGCPStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceGCPStorage();
        serviceCollection.AddScoped<IStorage, FakeAzureStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceGCPStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IGCPStorage>();
        serviceCollection.RemoveAll<GCPStorage>();
        serviceCollection.AddScoped<IGCPStorage, FakeGCPStorage>();
        return serviceCollection;
    }
}