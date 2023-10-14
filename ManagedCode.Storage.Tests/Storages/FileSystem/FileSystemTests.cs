using System;
using System.IO;
using FluentAssertions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.FileSystem;

public class FileSystemTests 
{
  
    [Fact]
    public void StorageAsDefaultTest()
    {
        var storage = FileSystemConfigurator.ConfigureServices("test").GetService<IFileSystemStorage>();
        var defaultStorage = FileSystemConfigurator.ConfigureServices("test").GetService<IStorage>();
        storage?.GetType().FullName.Should().Be(defaultStorage?.GetType().FullName);
    }
}

public class FileSystemConfigurator
{
    public static ServiceProvider ConfigureServices(string connectionString)
    {

        var services = new ServiceCollection();

        services.AddFileSystemStorageAsDefault(opt => { opt.BaseFolder = Path.Combine(Environment.CurrentDirectory,connectionString); });

        services.AddFileSystemStorage(new FileSystemStorageOptions
        {
            BaseFolder = Path.Combine(Environment.CurrentDirectory, connectionString)
        });
        return services.BuildServiceProvider();
    }
}