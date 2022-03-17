using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.FileSystem.Builders;

public class FileSystemProviderBuilder : ProviderBuilder
{
    public FileSystemProviderBuilder(
        IServiceCollection serviceCollection,
        string commonPath) : base(serviceCollection)
    {
        _commonPath = commonPath;
    }

    private string _commonPath { get; }

    public FileSystemProviderBuilder Add<TFileStorage>(Action<PathOptions> action)
        where TFileStorage : IStorage
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