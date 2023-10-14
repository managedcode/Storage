using FluentAssertions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

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