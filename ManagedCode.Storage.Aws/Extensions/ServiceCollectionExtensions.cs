using System;
using ManagedCode.Storage.Aws.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Aws.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAWSStorage(
        this IServiceCollection serviceCollection,
        Action<AWSStorageOptions> action)
    {
        var awsStorageOptions = new AWSStorageOptions();
        action.Invoke(awsStorageOptions);

        return serviceCollection
            .AddScoped<IAWSStorage>(_ => new AWSStorage(awsStorageOptions));
    }
}