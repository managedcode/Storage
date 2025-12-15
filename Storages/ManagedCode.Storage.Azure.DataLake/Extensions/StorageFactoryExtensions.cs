using System;
using ManagedCode.Storage.Azure.DataLake.Options;
using ManagedCode.Storage.Core.Providers;

namespace ManagedCode.Storage.Azure.DataLake.Extensions;

public static class StorageFactoryExtensions
{
    public static IAzureDataLakeStorage CreateAzureDataLakeStorage(this IStorageFactory factory, string fileSystemName)
    {
        return factory.CreateStorage<IAzureDataLakeStorage, AzureDataLakeStorageOptions>(options => options.FileSystem = fileSystemName);
    }

    public static IAzureDataLakeStorage CreateAzureDataLakeStorage(this IStorageFactory factory, AzureDataLakeStorageOptions options)
    {
        return factory.CreateStorage<IAzureDataLakeStorage, AzureDataLakeStorageOptions>(options);
    }


    public static IAzureDataLakeStorage CreateAzureDataLakeStorage(this IStorageFactory factory, Action<AzureDataLakeStorageOptions> options)
    {
        return factory.CreateStorage<IAzureDataLakeStorage, AzureDataLakeStorageOptions>(options);
    }
}