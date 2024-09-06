using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.DataLake;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.Google;
using ManagedCode.Storage.WebApi.Services.Abstractions;

namespace ManagedCode.Storage.WebApi.Services.Implementations;

public class StorageFactory(IServiceProvider provider) : IStorageFactory
{
    public IStorage GetStorage(StorageType storageType)
    {
        return storageType switch
        {
            StorageType.Aws => provider.GetRequiredService<IAWSStorage>(),
            StorageType.Azure => provider.GetRequiredService<IAzureStorage>(),
            StorageType.DataLake => provider.GetRequiredService<IAzureDataLakeStorage>(),
            StorageType.FileSystem => provider.GetRequiredService<IFileSystemStorage>(),
            StorageType.Google => provider.GetRequiredService<IGCPStorage>(),
            _ => throw new ArgumentOutOfRangeException(nameof(storageType), storageType, "Storage name out of range")
        };
    }
}