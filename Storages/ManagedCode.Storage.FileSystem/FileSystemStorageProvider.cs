using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.FileSystem
{
    public class FileSystemStorageProvider(IServiceProvider serviceProvider, FileSystemStorageOptions defaultOptions) : IStorageProvider
    {
        public Type StorageOptionsType => typeof(FileSystemStorageOptions);

        public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
            where TStorage : class, IStorage
            where TOptions : class, IStorageOptions
        {
            if (options is not FileSystemStorageOptions azureOptions)
            {
                throw new ArgumentException($"Options must be of type {typeof(FileSystemStorageOptions)}", nameof(options));
            }

            //var logger = serviceProvider.GetService<ILogger<FileSystemStorage>>();
            var storage = new FileSystemStorage(azureOptions);

            return storage as TStorage
                   ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
        }

        public IStorageOptions GetDefaultOptions()
        {
            return new FileSystemStorageOptions
            {
                BaseFolder = defaultOptions.BaseFolder,
                CreateContainerIfNotExists = defaultOptions.CreateContainerIfNotExists
            };
        }
    }
}
