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

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        var result = await Storage.ExistsAsync("random.txt");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        await Storage.UploadAsync("a.txt", "test content for a.txt");
        var stream = await Storage.DownloadAsStreamAsync("a.txt");
        stream.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        await Storage.UploadAsync("a1.txt", "test content for a1.txt");
        var tempFile = await Storage.DownloadAsync("a1.txt");
        using var sr = new StreamReader(tempFile.FileStream, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenUploadAsyncIsCalled()
    {
        var lineToUpload = "some text";

        var byteArray = Encoding.ASCII.GetBytes(lineToUpload);
        var stream = new MemoryStream(byteArray);

        await Storage.UploadStreamAsync("b.txt", stream);
    }

    [Fact]
    public async Task WhenDeleteAsyncIsCalled()
    {
        await Storage.DeleteAsync("a.txt");
    }
}