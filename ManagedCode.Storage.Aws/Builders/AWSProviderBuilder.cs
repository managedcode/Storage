using System;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Aws.Builders;

public class AWSProviderBuilder : ProviderBuilder
{
    private readonly AuthOptions _authOptions;

    public AWSProviderBuilder(
        IServiceCollection serviceCollection,
        AuthOptions authOptions) : base(serviceCollection)
    {
        _authOptions = authOptions;
    }

    public AWSProviderBuilder Add<TAWSStorage>(Action<BucketOptions> action)
        where TAWSStorage : IStorage
    {
        var bucketOptions = new BucketOptions();
        action.Invoke(bucketOptions);

        var storageOptions = new StorageOptions
        {
            PublicKey = _authOptions.PublicKey,
            SecretKey = _authOptions.SecretKey,
            Bucket = bucketOptions.Bucket
        };

        var implementationType = TypeHelpers.GetImplementationType<TAWSStorage, AWSStorage, StorageOptions>();
        ServiceCollection.AddScoped(typeof(TAWSStorage), x => Activator.CreateInstance(implementationType, storageOptions));

        return this;
    }
}