using System;
using Microsoft.Extensions.DependencyInjection;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.FileSystem.Builders
{
    public class FileSystemProviderBuilder : ProviderBuilder
    {
        private string _commonPath { get; set; }

        public FileSystemProviderBuilder(
            IServiceCollection serviceCollection,
            string commonPath) : base(serviceCollection)
        {
            _commonPath = commonPath;
        }

        public FileSystemProviderBuilder Add<TFileStorage>(Action<PathOptions> action)
            where TFileStorage : IBlobStorage
        {
            var pathOptions = new PathOptions();
            action.Invoke(pathOptions);

            var storageOptions = new StorageOptions
            {
                CommonPath = _commonPath,
                Path = pathOptions.Path
            };

            var implementationType = TypeHelpers.GetImplementationType<TFileStorage, FileSystemStorage, StorageOptions>();
            ServiceCollection.AddScoped(typeof(TFileStorage), x => Activator.CreateInstance(implementationType, storageOptions));

            return this;
        }
    }
}
