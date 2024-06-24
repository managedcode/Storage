using System;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Aws.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAWSStorage(this IServiceCollection serviceCollection, Action<AWSStorageOptions> action)
    {
        var options = new AWSStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAWSStorage(options);
    }

    public static IServiceCollection AddAWSStorageAsDefault(this IServiceCollection serviceCollection, Action<AWSStorageOptions> action)
    {
        var options = new AWSStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddAWSStorageAsDefault(options);
    }

    public static IServiceCollection AddAWSStorage(this IServiceCollection serviceCollection, AWSStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        return serviceCollection.AddTransient<IAWSStorage, AWSStorage>();
    }

    public static IServiceCollection AddAWSStorageAsDefault(this IServiceCollection serviceCollection, AWSStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddTransient<IAWSStorage, AWSStorage>();
        return serviceCollection.AddTransient<IStorage, AWSStorage>();
    }

    private static void CheckConfiguration(AWSStorageOptions options)
    {
        if (string.IsNullOrEmpty(options.PublicKey))
            throw new BadConfigurationException($"{nameof(options.PublicKey)} cannot be empty");

        if (string.IsNullOrEmpty(options.SecretKey))
            throw new BadConfigurationException($"{nameof(options.SecretKey)} cannot be empty");

        if (string.IsNullOrEmpty(options.Bucket))
            throw new BadConfigurationException($"{nameof(options.Bucket)} cannot be empty");
    }
}