using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.OneDrive.Options;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.OneDrive;

public class OneDriveStorageProvider(IServiceProvider serviceProvider, OneDriveStorageOptions defaultOptions) : IStorageProvider
{
    public Type StorageOptionsType => typeof(OneDriveStorageOptions);

    public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
        where TStorage : class, IStorage
        where TOptions : class, IStorageOptions
    {
        if (options is not OneDriveStorageOptions driveOptions)
        {
            throw new ArgumentException($"Options must be of type {typeof(OneDriveStorageOptions)}", nameof(options));
        }

        var logger = serviceProvider.GetService(typeof(ILogger<OneDriveStorage>)) as ILogger<OneDriveStorage>;
        var storage = new OneDriveStorage(driveOptions, logger);

        return storage as TStorage ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
    }

    public IStorageOptions GetDefaultOptions()
    {
        return new OneDriveStorageOptions
        {
            DriveId = defaultOptions.DriveId,
            RootPath = defaultOptions.RootPath,
            GraphClient = defaultOptions.GraphClient,
            Client = defaultOptions.Client,
            CreateContainerIfNotExists = defaultOptions.CreateContainerIfNotExists
        };
    }
}
