using System;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        serviceCollection.AddSingleton<IStorageProvider, AWSStorageProvider>();
        return serviceCollection.AddSingleton<IAWSStorage, AWSStorage>();
    }

    public static IServiceCollection AddAWSStorageAsDefault(this IServiceCollection serviceCollection, AWSStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, AWSStorageProvider>();
        serviceCollection.AddSingleton<IAWSStorage, AWSStorage>();
        return serviceCollection.AddSingleton<IStorage, AWSStorage>();
    }

    public static IServiceCollection AddAWSStorage(this IServiceCollection serviceCollection, string key, Action<AWSStorageOptions> action)
    {
        var options = new AWSStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);
    
        serviceCollection.AddKeyedSingleton<AWSStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IAWSStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<AWSStorageOptions>(k);
            return new AWSStorage(opts);
        });

        return serviceCollection;
    }

    public static IServiceCollection AddAWSStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<AWSStorageOptions> action)
    {
        var options = new AWSStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);
    
        serviceCollection.AddKeyedSingleton<AWSStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IAWSStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<AWSStorageOptions>(k);
            return new AWSStorage(opts);
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IAWSStorage>(k));

        return serviceCollection;
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