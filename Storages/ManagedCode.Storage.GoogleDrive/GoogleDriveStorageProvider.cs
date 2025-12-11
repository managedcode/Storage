using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.GoogleDrive.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.GoogleDrive;

/// <summary>
/// Provider for creating Google Drive storage instances.
/// </summary>
public class GoogleDriveStorageProvider(IServiceProvider serviceProvider, GoogleDriveStorageOptions defaultOptions) : IStorageProvider
{
    public Type StorageOptionsType => typeof(GoogleDriveStorageOptions);

    public TStorage CreateStorage<TStorage, TOptions>(TOptions options)
        where TStorage : class, IStorage
        where TOptions : class, IStorageOptions
    {
        if (options is not GoogleDriveStorageOptions googleDriveOptions)
        {
            throw new ArgumentException($"Options must be of type {typeof(GoogleDriveStorageOptions)}", nameof(options));
        }

        var logger = serviceProvider.GetService<ILogger<GoogleDriveStorage>>();
        var storage = new GoogleDriveStorage(googleDriveOptions, logger);

        return storage as TStorage
               ?? throw new InvalidOperationException($"Cannot create storage of type {typeof(TStorage)}");
    }

    public IStorageOptions GetDefaultOptions()
    {
        return new GoogleDriveStorageOptions
        {
            FolderId = defaultOptions.FolderId,
            Credential = defaultOptions.Credential,
            ServiceAccountJsonPath = defaultOptions.ServiceAccountJsonPath,
            ServiceAccountJson = defaultOptions.ServiceAccountJson,
            CreateContainerIfNotExists = defaultOptions.CreateContainerIfNotExists,
            ApplicationName = defaultOptions.ApplicationName
        };
    }
}


