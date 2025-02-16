using System;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Google.Options;

namespace ManagedCode.Storage.Google.Extensions;

public static class StorageFactoryExtensions
{
    public static IGCPStorage CreateGCPStorage(this IStorageFactory factory, string containerName)
    {
        return factory.CreateStorage<IGCPStorage,GCPStorageOptions>(options => options.BucketOptions.Bucket = containerName);
    }
    
    public static IGCPStorage CreateGCPStorage(this IStorageFactory factory, GCPStorageOptions options)
    {
        return factory.CreateStorage<IGCPStorage,GCPStorageOptions>(options);
    }
    
    
    public static IGCPStorage CreateGCPStorage(this IStorageFactory factory, Action<GCPStorageOptions> options)
    {
        return factory.CreateStorage<IGCPStorage,GCPStorageOptions>(options);
    }
}