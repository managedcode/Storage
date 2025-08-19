using System;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.Ftp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.Ftp.Extensions;

/// <summary>
/// Extension methods for registering FTP storage services.
/// </summary>
public static class ServiceCollectionExtensions
{
    #region FTP Extensions

    public static IServiceCollection AddFtpStorage(this IServiceCollection serviceCollection, Action<FtpStorageOptions> action)
    {
        var options = new FtpStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddFtpStorage(options);
    }

    public static IServiceCollection AddFtpStorageAsDefault(this IServiceCollection serviceCollection, Action<FtpStorageOptions> action)
    {
        var options = new FtpStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddFtpStorageAsDefault(options);
    }

    public static IServiceCollection AddFtpStorage(this IServiceCollection serviceCollection, FtpStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton<IFtpStorageOptions>(options);
        serviceCollection.AddSingleton<IStorageProvider, FtpStorageProvider>();
        return serviceCollection.AddSingleton<IFtpStorage, FtpStorage>();
    }

    public static IServiceCollection AddFtpStorageAsDefault(this IServiceCollection serviceCollection, FtpStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton<IFtpStorageOptions>(options);
        serviceCollection.AddSingleton<IStorageProvider, FtpStorageProvider>();
        serviceCollection.AddSingleton<IFtpStorage, FtpStorage>();
        return serviceCollection.AddSingleton<IStorage, FtpStorage>();
    }

    public static IServiceCollection AddFtpStorage(this IServiceCollection serviceCollection, string key, Action<FtpStorageOptions> action)
    {
        var options = new FtpStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<FtpStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IFtpStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<FtpStorageOptions>(k);
            var logger = sp.GetRequiredService<ILogger<FtpStorage>>();
            return new FtpStorage(opts!, logger);
        });

        return serviceCollection;
    }

    public static IServiceCollection AddFtpStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<FtpStorageOptions> action)
    {
        var options = new FtpStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<FtpStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IFtpStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<FtpStorageOptions>(k);
            var logger = sp.GetRequiredService<ILogger<FtpStorage>>();
            return new FtpStorage(opts!, logger);
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IFtpStorage>(k));

        return serviceCollection;
    }

    #endregion

    #region FTPS Extensions

    public static IServiceCollection AddFtpsStorage(this IServiceCollection serviceCollection, Action<FtpsStorageOptions> action)
    {
        var options = new FtpsStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddFtpsStorage(options);
    }

    public static IServiceCollection AddFtpsStorageAsDefault(this IServiceCollection serviceCollection, Action<FtpsStorageOptions> action)
    {
        var options = new FtpsStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddFtpsStorageAsDefault(options);
    }

    public static IServiceCollection AddFtpsStorage(this IServiceCollection serviceCollection, FtpsStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton<IFtpStorageOptions>(options);
        serviceCollection.AddSingleton<IStorageProvider, FtpStorageProvider>();
        return serviceCollection.AddSingleton<IFtpStorage, FtpStorage>();
    }

    public static IServiceCollection AddFtpsStorageAsDefault(this IServiceCollection serviceCollection, FtpsStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton<IFtpStorageOptions>(options);
        serviceCollection.AddSingleton<IStorageProvider, FtpStorageProvider>();
        serviceCollection.AddSingleton<IFtpStorage, FtpStorage>();
        return serviceCollection.AddSingleton<IStorage, FtpStorage>();
    }

    public static IServiceCollection AddFtpsStorage(this IServiceCollection serviceCollection, string key, Action<FtpsStorageOptions> action)
    {
        var options = new FtpsStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<FtpsStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IFtpStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<FtpsStorageOptions>(k);
            var logger = sp.GetRequiredService<ILogger<FtpStorage>>();
            return new FtpStorage(opts!, logger);
        });

        return serviceCollection;
    }

    public static IServiceCollection AddFtpsStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<FtpsStorageOptions> action)
    {
        var options = new FtpsStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<FtpsStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IFtpStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<FtpsStorageOptions>(k);
            var logger = sp.GetRequiredService<ILogger<FtpStorage>>();
            return new FtpStorage(opts!, logger);
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IFtpStorage>(k));

        return serviceCollection;
    }

    #endregion

    #region SFTP Extensions

    public static IServiceCollection AddSftpStorage(this IServiceCollection serviceCollection, Action<SftpStorageOptions> action)
    {
        var options = new SftpStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddSftpStorage(options);
    }

    public static IServiceCollection AddSftpStorageAsDefault(this IServiceCollection serviceCollection, Action<SftpStorageOptions> action)
    {
        var options = new SftpStorageOptions();
        action.Invoke(options);

        CheckConfiguration(options);

        return serviceCollection.AddSftpStorageAsDefault(options);
    }

    public static IServiceCollection AddSftpStorage(this IServiceCollection serviceCollection, SftpStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton<IFtpStorageOptions>(options);
        serviceCollection.AddSingleton<IStorageProvider, FtpStorageProvider>();
        return serviceCollection.AddSingleton<IFtpStorage, FtpStorage>();
    }

    public static IServiceCollection AddSftpStorageAsDefault(this IServiceCollection serviceCollection, SftpStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton<IFtpStorageOptions>(options);
        serviceCollection.AddSingleton<IStorageProvider, FtpStorageProvider>();
        serviceCollection.AddSingleton<IFtpStorage, FtpStorage>();
        return serviceCollection.AddSingleton<IStorage, FtpStorage>();
    }

    public static IServiceCollection AddSftpStorage(this IServiceCollection serviceCollection, string key, Action<SftpStorageOptions> action)
    {
        var options = new SftpStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<SftpStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IFtpStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<SftpStorageOptions>(k);
            var logger = sp.GetRequiredService<ILogger<FtpStorage>>();
            return new FtpStorage(opts!, logger);
        });

        return serviceCollection;
    }

    public static IServiceCollection AddSftpStorageAsDefault(this IServiceCollection serviceCollection, string key, Action<SftpStorageOptions> action)
    {
        var options = new SftpStorageOptions();
        action.Invoke(options);
        CheckConfiguration(options);

        serviceCollection.AddKeyedSingleton<SftpStorageOptions>(key, options);
        serviceCollection.AddKeyedSingleton<IFtpStorage>(key, (sp, k) =>
        {
            var opts = sp.GetKeyedService<SftpStorageOptions>(k);
            var logger = sp.GetRequiredService<ILogger<FtpStorage>>();
            return new FtpStorage(opts!, logger);
        });
        serviceCollection.AddKeyedSingleton<IStorage>(key, (sp, k) =>
            sp.GetRequiredKeyedService<IFtpStorage>(k));

        return serviceCollection;
    }

    #endregion

    #region Generic Extensions

    public static IServiceCollection AddFtpStorage(this IServiceCollection serviceCollection, IFtpStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, FtpStorageProvider>();
        return serviceCollection.AddSingleton<IFtpStorage, FtpStorage>();
    }

    public static IServiceCollection AddFtpStorageAsDefault(this IServiceCollection serviceCollection, IFtpStorageOptions options)
    {
        CheckConfiguration(options);
        serviceCollection.AddSingleton(options);
        serviceCollection.AddSingleton<IStorageProvider, FtpStorageProvider>();
        serviceCollection.AddSingleton<IFtpStorage, FtpStorage>();
        return serviceCollection.AddSingleton<IStorage, FtpStorage>();
    }

    #endregion

    private static void CheckConfiguration(IFtpStorageOptions options)
    {
        if (string.IsNullOrEmpty(options.Host))
            throw new BadConfigurationException($"{nameof(options.Host)} cannot be empty");

        if (options.Port <= 0)
            throw new BadConfigurationException($"{nameof(options.Port)} must be a positive number");

        if (string.IsNullOrEmpty(options.Username) && options is not FtpStorageOptions { Username: null })
            throw new BadConfigurationException($"{nameof(options.Username)} cannot be empty for authenticated connections");

        if (options is SftpStorageOptions sftpOptions)
        {
            var hasPassword = !string.IsNullOrEmpty(sftpOptions.Password);
            var hasPrivateKey = !string.IsNullOrEmpty(sftpOptions.PrivateKeyPath) || 
                               !string.IsNullOrEmpty(sftpOptions.PrivateKeyContent);

            if (!hasPassword && !hasPrivateKey)
                throw new BadConfigurationException("SFTP requires either password or private key authentication");
        }

        if (options is FtpsStorageOptions ftpsOptions)
        {
            if (!string.IsNullOrEmpty(ftpsOptions.ClientCertificatePath) && 
                !System.IO.File.Exists(ftpsOptions.ClientCertificatePath))
                throw new BadConfigurationException($"Client certificate file not found: {ftpsOptions.ClientCertificatePath}");
        }

        if (string.IsNullOrEmpty(options.RemoteDirectory))
        {
            options.RemoteDirectory = "/";
        }
    }
}