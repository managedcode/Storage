using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Sftp;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Sftp;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Sftp;

/// <summary>
/// Additional integration tests for the SFTP storage provider.
/// </summary>
public class SftpSpecificTests : BaseContainer<SftpContainer>
{
    protected override SftpContainer Build() => SftpContainerFactory.Create();

    protected override ServiceProvider ConfigureServices()
    {
        return SftpConfigurator.ConfigureServices(
            Container.GetHost(),
            Container.GetPort(),
            SftpContainerFactory.Username,
            SftpContainerFactory.Password,
            SftpContainerFactory.RemoteDirectory);
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnSuccess()
    {
        var storage = ServiceProvider.GetRequiredService<ISftpStorage>();
        var result = await storage.TestConnectionAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task GetWorkingDirectoryAsync_ShouldReturnDirectory()
    {
        var storage = ServiceProvider.GetRequiredService<ISftpStorage>();
        var result = await storage.GetWorkingDirectoryAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangeWorkingDirectoryAsync_ShouldSucceed()
    {
        var storage = ServiceProvider.GetRequiredService<ISftpStorage>();
        var result = await storage.ChangeWorkingDirectoryAsync(SftpContainerFactory.RemoteDirectory);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAndDownloadUsingStreams_ShouldMatch()
    {
        var storage = ServiceProvider.GetRequiredService<ISftpStorage>();
        var fileName = "stream-test.txt";
        var content = "Stream based upload";

        await using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var writeResult = await storage.OpenWriteStreamAsync(fileName);
        writeResult.IsSuccess.Should().BeTrue();
        var destinationStream = writeResult.Value ?? throw new InvalidOperationException("Write stream is null");

        await using (destinationStream)
        {
            await uploadStream.CopyToAsync(destinationStream);
        }

        var readResult = await storage.OpenReadStreamAsync(fileName);
        readResult.IsSuccess.Should().BeTrue();
        var sourceStream = readResult.Value ?? throw new InvalidOperationException("Read stream is null");
        using var reader = new StreamReader(sourceStream);
        var downloadedContent = await reader.ReadToEndAsync();

        downloadedContent.Should().Be(content);
    }

    [Fact]
    public async Task UploadFile_ShouldAppearInListing()
    {
        var storage = ServiceProvider.GetRequiredService<ISftpStorage>();
        var fileName = "list-test.txt";

        var uploadResult = await storage.UploadAsync("List test", options => options.FileName = fileName);
        uploadResult.IsSuccess.Should().BeTrue();

        var found = false;
        await foreach (var item in storage.GetBlobMetadataListAsync())
        {
            if (item.Name == fileName)
            {
                found = true;
                break;
            }
        }

        found.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDirectoryAsync_ShouldRemoveDirectory()
    {
        var storage = ServiceProvider.GetRequiredService<ISftpStorage>();
        var directory = "temp-dir";
        var fileName = "temp.txt";

        await storage.UploadAsync("Hello", options =>
        {
            options.FileName = fileName;
            options.Directory = directory;
        });

        var deleteResult = await storage.DeleteDirectoryAsync(directory);
        deleteResult.IsSuccess.Should().BeTrue();

        var existsResult = await storage.ExistsAsync(new ExistOptions
        {
            Directory = directory,
            FileName = fileName
        });

        existsResult.IsSuccess.Should().BeTrue();
        existsResult.Value.Should().BeFalse();
    }

    [Fact]
    public async Task UploadLargeFile_ShouldSucceed()
    {
        var storage = ServiceProvider.GetRequiredService<ISftpStorage>();
        var fileName = "large-file.bin";
        var bytes = new byte[1024 * 1024];
        new Random().NextBytes(bytes);

        var result = await storage.UploadAsync(bytes, options => options.FileName = fileName);
        result.IsSuccess.Should().BeTrue();
    }
}
