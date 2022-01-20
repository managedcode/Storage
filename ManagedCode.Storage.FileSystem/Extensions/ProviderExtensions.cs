using System;
using ManagedCode.Storage.Core.Builders;
using ManagedCode.Storage.FileSystem.Builders;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.FileSystem.Extensions
{
    public static class ProviderExtensions
    {
        public static FileSystemProviderBuilder AddFileSystemStorage(
            this ProviderBuilder providerBuilder,
            Action<CommonPathOptions> action)
        {
            var commonPathOptions = new CommonPathOptions();
            action.Invoke(commonPathOptions);

            return new FileSystemProviderBuilder(providerBuilder.ServiceCollection, commonPathOptions.Path);
        }
    }
}
