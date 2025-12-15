using System;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.FileSystem.Extensions;

public static class StorageFactoryExtensions
{
    public static IFileSystemStorage CreateFileSystemStorage(this IStorageFactory factory, string baseFolder)
    {
        return factory.CreateStorage<IFileSystemStorage, FileSystemStorageOptions>(options => options.BaseFolder = baseFolder);
    }

    public static IFileSystemStorage CreateFileSystemStorage(this IStorageFactory factory, FileSystemStorageOptions options)
    {
        return factory.CreateStorage<IFileSystemStorage, FileSystemStorageOptions>(options);
    }


    public static IFileSystemStorage CreateFileSystemStorage(this IStorageFactory factory, Action<FileSystemStorageOptions> options)
    {
        return factory.CreateStorage<IFileSystemStorage, FileSystemStorageOptions>(options);
    }
}