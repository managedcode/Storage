using System;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core.Providers;

namespace ManagedCode.Storage.Aws.Extensions;

public static class StorageFactoryExtensions
{
    public static IAWSStorage CreateAWSStorage(this IStorageFactory factory, string bucketName)
    {
        return factory.CreateStorage<IAWSStorage, AWSStorageOptions>(options => options.Bucket = bucketName);
    }

    public static IAWSStorage CreateAWSStorage(this IStorageFactory factory, AWSStorageOptions options)
    {
        return factory.CreateStorage<IAWSStorage, AWSStorageOptions>(options);
    }


    public static IAWSStorage CreateAWSStorage(this IStorageFactory factory, Action<AWSStorageOptions> options)
    {
        return factory.CreateStorage<IAWSStorage, AWSStorageOptions>(options);
    }
}