using System;
using ManagedCode.Storage.Core;

namespace Storage
{
    public interface IStorageFactory
    {
        IStorage CreateStorage(IStorageOptions options);
        IStorage CreateStorage(Action<IStorageOptions> options);
        
        TStorage CreateStorage<TStorage,TOptions>(TOptions options) 
            where TStorage : class, IStorage 
            where TOptions : class, IStorageOptions;
        
        TStorage CreateStorage<TStorage,TOptions>(Action<TOptions> options) 
            where TStorage : class, IStorage 
            where TOptions : class, IStorageOptions;
    }
}
