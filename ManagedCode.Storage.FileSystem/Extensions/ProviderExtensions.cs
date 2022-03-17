using System;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.FileSystem.Builders;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.FileSystem.Extensions;

public static class ProviderExtensions
{
    public static FileSystemProviderBuilder AddFileSystemStorage(
        this ProviderBuilder providerBuilder,
        Action<PathOptions> action)
    {
        var commonPathOptions = new PathOptions();
        action.Invoke(commonPathOptions);

        return new FileSystemProviderBuilder(providerBuilder.ServiceCollection, commonPathOptions.Path);
    }
}