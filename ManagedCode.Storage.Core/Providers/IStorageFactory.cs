using System;

namespace ManagedCode.Storage.Core.Providers
{
    public interface IStorageFactory
    {
        IStorage CreateStorage(IStorageOptions options);
        IStorage CreateStorage(Action<IStorageOptions> options);

        TStorage CreateStorage<TStorage, TOptions>(TOptions options)
            where TStorage : class, IStorage
            where TOptions : class, IStorageOptions;

        TStorage CreateStorage<TStorage, TOptions>(Action<TOptions> options)
            where TStorage : class, IStorage
            where TOptions : class, IStorageOptions;
    }
}
