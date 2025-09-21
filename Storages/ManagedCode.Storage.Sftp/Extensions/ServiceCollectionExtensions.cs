using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Sftp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Sftp.Extensions;

/// <summary>
/// Service registration helpers for the SFTP storage provider.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSftpStorage(this IServiceCollection services, Action<SftpStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SftpStorageOptions();
        configure(options);
        CheckConfiguration(options);

        return services.AddSftpStorage(options);
    }

    public static IServiceCollection AddSftpStorageAsDefault(this IServiceCollection services, Action<SftpStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SftpStorageOptions();
        configure(options);
        CheckConfiguration(options);

        return services.AddSftpStorageAsDefault(options);
    }

    public static IServiceCollection AddSftpStorage(this IServiceCollection services, SftpStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        CheckConfiguration(options);

        services.AddSingleton(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStorageProvider, SftpStorageProvider>());
        services.AddSingleton<ISftpStorage>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SftpStorage>>();
            var opts = sp.GetRequiredService<SftpStorageOptions>();
            return new SftpStorage(opts, logger);
        });

        return services;
    }

    public static IServiceCollection AddSftpStorageAsDefault(this IServiceCollection services, SftpStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        CheckConfiguration(options);

        services.AddSftpStorage(options);
        services.AddSingleton<IStorage>(sp => sp.GetRequiredService<ISftpStorage>());
        return services;
    }

    public static IServiceCollection AddSftpStorage(this IServiceCollection services, string key, Action<SftpStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SftpStorageOptions();
        configure(options);
        CheckConfiguration(options);

        services.AddKeyedSingleton(key, options);
        services.AddKeyedSingleton<ISftpStorage>(key, (sp, k) =>
        {
            var opts = sp.GetRequiredKeyedService<SftpStorageOptions>(k);
            var logger = sp.GetRequiredService<ILogger<SftpStorage>>();
            return new SftpStorage(opts, logger);
        });

        return services;
    }

    public static IServiceCollection AddSftpStorageAsDefault(this IServiceCollection services, string key, Action<SftpStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSftpStorage(key, configure);
        services.AddKeyedSingleton<IStorage>(key, (sp, k) => sp.GetRequiredKeyedService<ISftpStorage>(k));
        return services;
    }

    private static void CheckConfiguration(SftpStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new BadConfigurationException("SFTP host is not configured.");
        }

        if (options.Port <= 0)
        {
            throw new BadConfigurationException("SFTP port must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            throw new BadConfigurationException("SFTP username is not configured.");
        }

        var hasPassword = !string.IsNullOrWhiteSpace(options.Password);
        var hasKey = !string.IsNullOrWhiteSpace(options.PrivateKeyPath) || !string.IsNullOrWhiteSpace(options.PrivateKeyContent);

        if (!hasPassword && !hasKey)
        {
            throw new BadConfigurationException("SFTP storage requires either a password or key-based credentials.");
        }
    }
}
