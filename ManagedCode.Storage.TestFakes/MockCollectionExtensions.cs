using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.DataLake;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ManagedCode.Storage.TestFakes;

public static class MockCollectionExtensions
{
    public static IServiceCollection ReplaceAWSStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceAWSStorage();
        serviceCollection.AddSingleton<IStorage, FakeAWSStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAWSStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IAWSStorage>();
        serviceCollection.RemoveAll<AWSStorage>();
        serviceCollection.AddSingleton<IAWSStorage, FakeAWSStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureDataLakeStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IAzureDataLakeStorage>();
        serviceCollection.RemoveAll<AzureDataLakeStorage>();
        serviceCollection.AddSingleton<IAzureDataLakeStorage, FakeAzureDataLakeStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureDataLakeStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceAzureDataLakeStorage();
        serviceCollection.AddSingleton<IStorage, FakeAzureDataLakeStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IAzureStorage>();
        serviceCollection.RemoveAll<FakeAzureStorage>();
        serviceCollection.AddSingleton<IAzureStorage, FakeAzureStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceAzureStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceAzureStorage();
        serviceCollection.AddSingleton<IStorage, FakeAzureStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceGoogleStorageAsDefault(this IServiceCollection serviceCollection)
    {
        serviceCollection.ReplaceGoogleStorage();
        serviceCollection.AddSingleton<IStorage, FakeAzureStorage>();
        return serviceCollection;
    }

    public static IServiceCollection ReplaceGoogleStorage(this IServiceCollection serviceCollection)
    {
        serviceCollection.RemoveAll<IGCPStorage>();
        serviceCollection.RemoveAll<GCPStorage>();
        serviceCollection.AddSingleton<IGCPStorage, FakeGoogleStorage>();
        return serviceCollection;
    }
}