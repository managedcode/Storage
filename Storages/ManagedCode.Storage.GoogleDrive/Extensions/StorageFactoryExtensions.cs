using System;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.GoogleDrive.Options;

namespace ManagedCode.Storage.GoogleDrive.Extensions;

/// <summary>
/// Extension methods for creating Google Drive storage instances via IStorageFactory.
/// </summary>
public static class StorageFactoryExtensions
{
    /// <summary>
    /// Creates a Google Drive storage instance with the specified folder ID.
    /// </summary>
    public static IGoogleDriveStorage CreateGoogleDriveStorage(this IStorageFactory factory, string folderId)
    {
        return factory.CreateStorage<IGoogleDriveStorage, GoogleDriveStorageOptions>(options => options.FolderId = folderId);
    }

    /// <summary>
    /// Creates a Google Drive storage instance with the specified options.
    /// </summary>
    public static IGoogleDriveStorage CreateGoogleDriveStorage(this IStorageFactory factory, GoogleDriveStorageOptions options)
    {
        return factory.CreateStorage<IGoogleDriveStorage, GoogleDriveStorageOptions>(options);
    }

    /// <summary>
    /// Creates a Google Drive storage instance with the specified options action.
    /// </summary>
    public static IGoogleDriveStorage CreateGoogleDriveStorage(this IStorageFactory factory, Action<GoogleDriveStorageOptions> options)
    {
        return factory.CreateStorage<IGoogleDriveStorage, GoogleDriveStorageOptions>(options);
    }
}


