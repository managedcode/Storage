using System;
using ManagedCode.Storage.Core;

namespace Storage
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
