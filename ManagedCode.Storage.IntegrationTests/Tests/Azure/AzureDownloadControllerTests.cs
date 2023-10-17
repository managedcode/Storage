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
    public async Task DownloadFile_WhenFileExists_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);
        var fileCRC = Crc32Helper.Calculate(await localFile.ReadAllBytesAsync());
        var uploadFileBlob = await storageClient.UploadFile(localFile.FileStream, ApiEndpoints.Azure.UploadFile, contentName);
        
        // Act
        var downloadResult = await storageClient.DownloadFile(uploadFileBlob.Value.FullName, ApiEndpoints.Azure.DownloadFile);

        // Assert
        downloadResult.Should().NotBeNull();
        var downloadedFile = await LocalFile.FromStreamAsync(downloadResult);
        var downloadedFileCRC = Crc32Helper.Calculate(await downloadedFile.ReadAllBytesAsync());
        downloadedFileCRC.Should().Be(fileCRC);
    }
}