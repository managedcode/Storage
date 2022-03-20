using System;
using System.IO;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.FileSystem;

public class FileSystemTests : StorageBaseTests
{
    public FileSystemTests()
    {
        var services = new ServiceCollection();

        var testDirectory = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket");

        services.AddFileSystemStorage(opt =>
        {
            opt.CommonPath = testDirectory;
            opt.Path = "managed-code-bucket";
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IFileSystemStorage>();
    }
}