using System;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.FileSystem.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFileSystemStorage(
        this IServiceCollection serviceCollection,
        Action<FSStorageOptions> action)
    {
        var awsStorageOptions = new FSStorageOptions();
        action.Invoke(awsStorageOptions);

        return serviceCollection
            .AddScoped<IFileSystemStorage>(_ => new FileSystemStorage(awsStorageOptions));
    }
}