using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.GoogleDrive.Options;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.GoogleDrive;

public class GoogleDriveStorageProvider(IServiceProvider serviceProvider, GoogleDriveStorageOptions defaultOptions) : IStorageProvider
{
    public Type StorageOptionsType => typeof(GoogleDriveStorageOptions);

    public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
        where TStorage : class, IStorage
        where TOptions : class, IStorageOptions
    {
        if (options is not GoogleDriveStorageOptions driveOptions)
        {
            throw new ArgumentException($"Options must be of type {typeof(GoogleDriveStorageOptions)}", nameof(options));
        }

        var logger = serviceProvider.GetService(typeof(ILogger<GoogleDriveStorage>)) as ILogger<GoogleDriveStorage>;
        var storage = new GoogleDriveStorage(driveOptions, logger);
        return storage as TStorage ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
    }

    public IStorageOptions GetDefaultOptions()
    {
        return new GoogleDriveStorageOptions
        {
            RootFolderId = defaultOptions.RootFolderId,
            DriveService = defaultOptions.DriveService,
            Client = defaultOptions.Client,
            CreateContainerIfNotExists = defaultOptions.CreateContainerIfNotExists
        };
    }
}
