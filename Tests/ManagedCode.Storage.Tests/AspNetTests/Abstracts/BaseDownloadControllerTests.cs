using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Constants;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Abstracts;

public abstract class BaseDownloadControllerTests : BaseControllerTests
{
    private readonly string _downloadEndpoint;
    private readonly string _downloadBytesEndpoint;
    private readonly string _uploadEndpoint;

    protected BaseDownloadControllerTests(StorageTestApplication testApplication, string apiEndpoint) : base(testApplication, apiEndpoint)
    {
        _uploadEndpoint = string.Format(ApiEndpoints.Base.UploadFile, ApiEndpoint);
        _downloadEndpoint = $"{ApiEndpoint}/download";
        _downloadBytesEndpoint = $"{ApiEndpoint}/download-bytes";
    }

    [Fact]
    public async Task DownloadFile_WhenFileExists_SaveToTempStorage_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFileWithData(localFile, 100); // Generate file with actual data
        var fileCRC = Crc32Helper.CalculateFileCrc(localFile.FilePath); // Calculate CRC from file path
        await using var uploadStream = localFile.FileStream; // Get stream once
        var uploadFileBlob = await storageClient.UploadFile(uploadStream, _uploadEndpoint, contentName);

        // Act
        var downloadedFileResult = await storageClient.DownloadFile(uploadFileBlob.Value.FullName, _downloadEndpoint);

        // Assert
        downloadedFileResult.IsSuccess
            .Should()
            .BeTrue();
        downloadedFileResult.Value
            .Should()
            .NotBeNull();
        var downloadedFileCRC = Crc32Helper.CalculateFileCrc(downloadedFileResult.Value.FilePath);
        downloadedFileCRC.Should()
            .Be(fileCRC);
    }

    [Fact]
    public async Task DownloadFileAsBytes_WhenFileExists_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFileWithData(localFile, 100); // Generate file with actual data
        var fileCRC = Crc32Helper.CalculateFileCrc(localFile.FilePath); // Calculate CRC from file path
        await using var uploadStream = localFile.FileStream; // Get stream once
        var uploadFileBlob = await storageClient.UploadFile(uploadStream, _uploadEndpoint, contentName);

        // Act
        var downloadedFileResult = await storageClient.DownloadFile(uploadFileBlob.Value.FullName, _downloadBytesEndpoint);

        // Assert
        downloadedFileResult.IsSuccess.Should().BeTrue();
        downloadedFileResult.Value.Should().NotBeNull();
        var downloadedFileCRC = Crc32Helper.CalculateFileCrc(downloadedFileResult.Value.FilePath);
        downloadedFileCRC.Should().Be(fileCRC);
    }

    [Fact]
    public async Task DownloadFile_WhenFileDoNotExist_ReturnFail()
    {
        // Arrange
        var storageClient = GetStorageClient();

        // Act
        var downloadedFileResult = await storageClient.DownloadFile(Guid.NewGuid()
            .ToString(), _downloadEndpoint);

        // Assert
        downloadedFileResult.IsFailed
            .Should()
            .BeTrue();
        downloadedFileResult.Problem
            ?.StatusCode
            .Should()
            .Be((int)HttpStatusCode.InternalServerError);
    }
}