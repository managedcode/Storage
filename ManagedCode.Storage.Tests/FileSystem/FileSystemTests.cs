using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.FileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.FileSystem;

public class FileSystemTests
{
    private readonly IDocumentStorage _documentStorage;
    private readonly IPhotoStorage _photoStorage;
    private readonly string _testDirectory;

    public FileSystemTests()
    {
        var services = new ServiceCollection();

        _testDirectory = Path.Combine(Environment.CurrentDirectory, "my_tests_files");

        services.AddFileSystemStorage(opt =>
        {
            opt.Path = _testDirectory;
            opt.Path = "documents";
        });

        var provider = services.BuildServiceProvider();

        _photoStorage = provider.GetService<IPhotoStorage>();
        _documentStorage = provider.GetService<IDocumentStorage>();
    }

    [Fact]
    public void WhenDIInitialized()
    {
        _photoStorage.Should().NotBeNull();
        _documentStorage.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenSingleBlobExistsIsCalled()
    {
        var result = await _documentStorage.ExistsAsync("random.txt");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WhenDownloadAsyncIsCalled()
    {
        await _documentStorage.UploadAsync("a.txt", "test content for a.txt");
        var stream = await _documentStorage.DownloadAsStreamAsync("a.txt");
        stream.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var content = await sr.ReadToEndAsync();

        content.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenDownloadAsyncToFileIsCalled()
    {
        await _documentStorage.UploadAsync("a1.txt", "test content for a1.txt");
        var tempFile = await _documentStorage.DownloadAsync("a1.txt");
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

        await _documentStorage.UploadStreamAsync("b.txt", stream);
    }

    [Fact]
    public async Task WhenDeleteAsyncIsCalled()
    {
        await _documentStorage.DeleteAsync("a.txt");
    }
}