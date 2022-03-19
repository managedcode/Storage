using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.FileSystem;

public class FileSystemTests : StorageBaseTests
{
    public FileSystemTests()
    {
        var services = new ServiceCollection();

        var testDirectory = Path.Combine(Environment.CurrentDirectory, "my_tests_files");

        services.AddFileSystemStorage(opt =>
        {
            opt.CommonPath = testDirectory;
            opt.Path = "documents";
        });

        var provider = services.BuildServiceProvider();

        Storage = provider.GetService<IFileSystemStorage>();
    }
}