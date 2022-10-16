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
        serviceCollection.AddTransient<IStorage, FakeAWSStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAWSStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IAWSStorage>();
        serviceCollection.RemoveAll<AWSStorage>();
        serviceCollection.AddTransient<IAWSStorage, FakeAWSStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureDataLakeStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IAzureDataLakeStorage>();
        serviceCollection.RemoveAll<AzureDataLakeStorage>();
        serviceCollection.AddTransient<IAzureDataLakeStorage, FakeAzureDataLakeStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureDataLakeStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceAzureDataLakeStorage();
        serviceCollection.AddTransient<IStorage, FakeAzureDataLakeStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IAzureStorage>();
        serviceCollection.RemoveAll<FakeAzureStorage>();
        serviceCollection.AddTransient<IAzureStorage, FakeAzureStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceAzureStorage();
        serviceCollection.AddTransient<IStorage, FakeAzureStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceGCPStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceGCPStorage();
        serviceCollection.AddTransient<IStorage, FakeAzureStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceGCPStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IGCPStorage>();
        serviceCollection.RemoveAll<GCPStorage>();
        serviceCollection.AddTransient<IGCPStorage, FakeGCPStorage>();
        return serviceCollection;
    }
}