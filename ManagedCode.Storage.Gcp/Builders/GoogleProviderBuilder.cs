using System;
using Google.Apis.Auth.OAuth2;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Gcp.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Gcp.Builders
{
    public class GoogleProviderBuilder : ProviderBuilder
    {
        private readonly GoogleCredential _googleCredential;

        public GoogleProviderBuilder(
            IServiceCollection serviceCollection,
            GoogleCredential googleCredential) : base(serviceCollection)
        {
            _googleCredential = googleCredential;
        }

        public GoogleProviderBuilder Add<TGoogleStorage>(Action<BucketOptions> action)
            where TGoogleStorage : IStorage
        {
            var bucketOptions = new BucketOptions();
            action.Invoke(bucketOptions);

            var storageOptions = new StorageOptions
            {
                GoogleCredential = _googleCredential,
                BucketOptions = bucketOptions
            };

            var implementationType = TypeHelpers.GetImplementationType<TGoogleStorage, GoogleStorage, StorageOptions>();
            ServiceCollection.AddScoped(typeof(TGoogleStorage), x => Activator.CreateInstance(implementationType, storageOptions));

            return this;
        }
    }
}