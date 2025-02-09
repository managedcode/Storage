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
        return serviceCollection.AddScoped<IAWSStorage, AWSStorage>();
    }

    public static IServiceCollection AddAWSStorageAsDefault(this IServiceCollection serviceCollection, AWSStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddScoped<IAWSStorage, AWSStorage>();
        return serviceCollection.AddScoped<IStorage, AWSStorage>();
    }

    public static IServiceCollection AddAWSStorage(this IServiceCollection serviceCollection, string key, Action<AWSStorageOptions> action)
    {
        var options = new AWSStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);
        
        serviceCollection.AddKeyedSingleton<AWSStorageOptions>(key, (_, _) => options);
        return serviceCollection.AddKeyedScoped<IAWSStorage, AWSStorage>(key);
    }

    public static IServiceCollection AddAWSStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<AWSStorageOptions> action)
    {
        var options = new AWSStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);
        
        serviceCollection.AddKeyedSingleton<AWSStorageOptions>(key, (_, _) => options);
        serviceCollection.AddKeyedScoped<IAWSStorage, AWSStorage>(key);
        return serviceCollection.AddKeyedScoped<IStorage, AWSStorage>(key);
    }

    private static void CheckConfiguration(AWSStorageOptions options)
    {
        // Make sure the bucket name is set.
        if (string.IsNullOrEmpty(options.Bucket))
            throw new BadConfigurationException($"{nameof(options.Bucket)} cannot be empty");

        // If we are using instance profile credentials, we don't need to check for the public and secret keys.
        if (!options.UseInstanceProfileCredentials)
        {
            if (string.IsNullOrEmpty(options.PublicKey))
                throw new BadConfigurationException($"{nameof(options.PublicKey)} cannot be empty");

            if (string.IsNullOrEmpty(options.SecretKey))
                throw new BadConfigurationException($"{nameof(options.SecretKey)} cannot be empty");
        }
    }
}