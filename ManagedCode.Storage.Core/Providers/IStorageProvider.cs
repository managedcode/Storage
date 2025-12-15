using System;

namespace ManagedCode.Storage.Core.Providers
{
    public interface IStorageProvider
    {
        Type StorageOptionsType { get; }
        TStorage CreateStorage<TStorage, TOptions>(TOptions options)
            where TStorage : class, IStorage
            where TOptions : class, IStorageOptions;


        IStorageOptions GetDefaultOptions();
    }
}
