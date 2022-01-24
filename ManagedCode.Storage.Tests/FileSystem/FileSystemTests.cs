using System;
using System.IO;
using FluentAssertions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.FileSystem;

public class FileSystemTests : StorageBaseTests
{ 
    protected override ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        services.AddFileSystemStorageAsDefault(opt =>
        {
            opt.CommonPath = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket");
            opt.Path = "managed-code-bucket";
        });
        
        services.AddFileSystemStorage(new FileSystemStorageOptions
        {
            CommonPath = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket"),
            Path = "managed-code-bucket"
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public void StorageAsDefaultTest()
    {
        var storage = ServiceProvider.GetService<IFileSystemStorage>();
        var defaultStorage = ServiceProvider.GetService<IStorage>();
        storage.GetType().FullName.Should().Be(defaultStorage.GetType().FullName);
    }
}