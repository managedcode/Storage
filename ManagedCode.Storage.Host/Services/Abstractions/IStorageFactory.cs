using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.WebApi.Services.Abstractions;

public interface IStorageFactory
{
    IStorage GetStorage(StorageType storageType);
}