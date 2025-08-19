using System;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Ftp;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Ftp;

/// <summary>
/// FTP-specific functionality tests.
/// </summary>
public class FtpSpecificTests : BaseContainer<FtpContainer>
{
    protected override FtpContainer Build()
    {
        return new FtpContainer();
    }

    protected override ServiceProvider ConfigureServices()
    {
        return FtpConfigurator.ConfigureServices(
            Container.GetHost(),
            Container.GetPort(),
            FtpContainer.Username,
            FtpContainer.Password,
            "/test-container");
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidConnection_ShouldReturnTrue()
    {
        // Arrange
        var ftpStorage = ServiceProvider.GetService<IFtpStorage>()!;

        // Act
        var result = await ftpStorage.TestConnectionAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task GetWorkingDirectoryAsync_ShouldReturnDirectory()
    {
        // Arrange
        var ftpStorage = ServiceProvider.GetService<IFtpStorage>()!;

        // Act
        var result = await ftpStorage.GetWorkingDirectoryAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangeWorkingDirectoryAsync_ShouldSucceed()
    {
        // Arrange
        var ftpStorage = ServiceProvider.GetService<IFtpStorage>()!;
        await ftpStorage.CreateContainerAsync(); // Ensure container exists

        // Act
        var result = await ftpStorage.ChangeWorkingDirectoryAsync("/test-container");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact(Skip = "Stream operations not supported with embedded FTP server - requires real FTP server with DATA connections")]
    public async Task OpenReadStreamAsync_WithExistingFile_ShouldReturnStream()
    {
        // Arrange
        var ftpStorage = ServiceProvider.GetService<IFtpStorage>()!;
        
        // Create a simple test file first with basic content
        var fileName = $"test-read-{Guid.NewGuid()}.txt";
        var testContent = "Simple test content for reading";
        var uploadResult = await Storage.UploadAsync(testContent, opt => opt.FileName = fileName);
        uploadResult.IsSuccess.Should().BeTrue("Upload should succeed");

        // Verify the file exists before trying to read
        var existsResult = await Storage.ExistsAsync(fileName);
        existsResult.IsSuccess.Should().BeTrue("Exists check should succeed");
        existsResult.Value.Should().BeTrue($"File {fileName} should exist after upload");

        // Act
        var result = await ftpStorage.OpenReadStreamAsync(fileName);

        // Assert
        if (!result.IsSuccess)
        {
            throw new Exception($"OpenReadStreamAsync failed - result was not successful");
        }
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CanRead.Should().BeTrue();

        // Cleanup
        await result.Value.DisposeAsync();
        await Storage.DeleteAsync(fileName);
    }

    [Fact(Skip = "Stream operations not supported with embedded FTP server - requires real FTP server with DATA connections")]
    public async Task OpenWriteStreamAsync_ShouldReturnWritableStream()
    {
        // Arrange
        var ftpStorage = ServiceProvider.GetService<IFtpStorage>()!;
        var fileName = $"test-write-{System.Guid.NewGuid()}.txt";

        // Act
        var result = await ftpStorage.OpenWriteStreamAsync(fileName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CanWrite.Should().BeTrue();

        // Write some data
        var data = System.Text.Encoding.UTF8.GetBytes("Test content");
        await result.Value!.WriteAsync(data);
        await result.Value.DisposeAsync();

        // Verify file was created
        var exists = await Storage.ExistsAsync(fileName);
        exists.IsSuccess.Should().BeTrue();
        exists.Value.Should().BeTrue();

        // Cleanup
        await Storage.DeleteAsync(fileName);
    }

    [Fact]
    public async Task CreateContainer_ShouldCreateRemoteDirectory()
    {
        // Arrange
        var ftpStorage = ServiceProvider.GetService<IFtpStorage>()!;

        // Act
        var result = await ftpStorage.CreateContainerAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveContainer_ShouldRemoveRemoteDirectory()
    {
        // Arrange
        var ftpStorage = ServiceProvider.GetService<IFtpStorage>()!;
        await ftpStorage.CreateContainerAsync();

        // Act
        var result = await ftpStorage.RemoveContainerAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UploadAndDownload_InSubDirectory_ShouldWork()
    {
        // Arrange
        var subDirectory = "subdirectory";
        var fileName = $"test-{System.Guid.NewGuid()}.txt";
        var content = "Test content for subdirectory";

        // Act - Upload
        var uploadResult = await Storage.UploadAsync(content, options =>
        {
            options.FileName = fileName;
            options.Directory = subDirectory;
        });

        // Assert - Upload
        uploadResult.IsSuccess.Should().BeTrue();

        // Act - Download
        var downloadResult = await Storage.DownloadAsync(options =>
        {
            options.FileName = fileName;
            options.Directory = subDirectory;
        });

        // Assert - Download
        downloadResult.IsSuccess.Should().BeTrue();
        var downloadedContent = downloadResult.Value!.ReadAllText();
        downloadedContent.Should().Be(content);

        // Cleanup
        await Storage.DeleteAsync(options =>
        {
            options.FileName = fileName;
            options.Directory = subDirectory;
        });
        
        downloadResult.Value.Dispose();
    }
}