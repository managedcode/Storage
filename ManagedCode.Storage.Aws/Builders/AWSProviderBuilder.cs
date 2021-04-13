using System;
using Microsoft.Extensions.DependencyInjection;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Core.Helpers;

namespace ManagedCode.Storage.Aws.Builders
{
    public class AWSProviderBuilder : ProviderBuilder
    {
        private AuthOptions _authOptions;

        public AWSProviderBuilder(
            IServiceCollection serviceCollection,
            AuthOptions authOptions) : base(serviceCollection)
        {
            _authOptions = authOptions;
        }

        public AWSProviderBuilder Add<TAWSStorage>(Action<BucketOptions> action)
            where TAWSStorage : IBlobStorage
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
}
