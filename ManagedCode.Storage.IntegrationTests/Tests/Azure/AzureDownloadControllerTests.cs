using System.Net;
using FluentAssertions;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.IntegrationTests.Constants;
using ManagedCode.Storage.IntegrationTests.Helpers;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests.Tests.Azure;

public class AzureDownloadControllerTests : BaseControllerTests
{
    public AzureDownloadControllerTests(StorageTestApplication testApplication) : base(testApplication)
    {
    }

    [Fact]
    public async Task DownloadFile_WhenFileExists_SaveToTempStorage_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);
        var fileCRC = Crc32Helper.Calculate(await localFile.ReadAllBytesAsync());
        var uploadFileBlob = await storageClient.UploadFile(localFile.FileStream, ApiEndpoints.Azure.UploadFile, contentName);
        
        // Act
        var downloadedFileResult = await storageClient.DownloadFile(uploadFileBlob.Value.FullName, ApiEndpoints.Azure.DownloadFile);

        // Assert
        downloadedFileResult.IsSuccess.Should().BeTrue();
        downloadedFileResult.Value.Should().NotBeNull();
        var downloadedFileCRC = Crc32Helper.Calculate(await downloadedFileResult.Value.ReadAllBytesAsync());
        downloadedFileCRC.Should().Be(fileCRC);
    }

    [Fact]
    public async Task DownloadFile_WhenFileDoNotExist_ReturnFail()
    {
        // Arrange
        var storageClient = GetStorageClient();;
        
        // Act
        var downloadedFileResult = await storageClient.DownloadFile(Guid.NewGuid().ToString(), ApiEndpoints.Azure.DownloadFile);

        // Assert
        downloadedFileResult.IsFailed.Should().BeTrue();
        downloadedFileResult.GetError().Value.ErrorCode.Should().Be(HttpStatusCode.InternalServerError.ToString());
    }
}