using System;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core.Providers;

namespace ManagedCode.Storage.Azure.Extensions;

public static class StorageFactoryExtensions
{
    public static IAzureStorage CreateAzureStorage(this IStorageFactory factory, string containerName)
    {
        return factory.CreateStorage<IAzureStorage, IAzureStorageOptions>(options => options.Container = containerName);
    }

    public static IAzureStorage CreateAzureStorage(this IStorageFactory factory, IAzureStorageOptions options)
    {
        return factory.CreateStorage<IAzureStorage, IAzureStorageOptions>(options);
    }


    public static IAzureStorage CreateAzureStorage(this IStorageFactory factory, Action<IAzureStorageOptions> options)
    {
        return factory.CreateStorage<IAzureStorage, IAzureStorageOptions>(options);
    }
}