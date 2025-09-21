using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Constants;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Abstracts;

public abstract class BaseUploadControllerTests : BaseControllerTests
{
    private readonly string _uploadEndpoint;
    private readonly string _uploadLargeFile;

    protected BaseUploadControllerTests(StorageTestApplication testApplication, string apiEndpoint) : base(testApplication, apiEndpoint)
    {
        _uploadEndpoint = string.Format(ApiEndpoints.Base.UploadFile, ApiEndpoint);
        _uploadLargeFile = string.Format(ApiEndpoints.Base.UploadLargeFile, ApiEndpoint);
    }

    [Fact]
    public async Task UploadFileFromStream_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);

        // Act
        var result = await storageClient.UploadFile(localFile.FileStream, _uploadEndpoint, contentName);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldNotBeNull();
    }

    [Fact(Skip = "There is no forbidden logic")]
    public async Task UploadFileFromStream_WhenFileSizeIsForbidden_ReturnFail()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 200);

        // Act
        var result = await storageClient.UploadFile(localFile.FileStream, _uploadEndpoint, contentName);

        // Assert
        result.IsFailed
            .ShouldBeTrue();
        result.Problem
            ?.StatusCode
            .ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadFileFromFileInfo_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);

        // Act
        var result = await storageClient.UploadFile(localFile.FileInfo, _uploadEndpoint, contentName);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task UploadFileFromBytes_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);

        var fileAsBytes = await localFile.ReadAllBytesAsync();

        // Act
        var result = await storageClient.UploadFile(fileAsBytes, _uploadEndpoint, contentName);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task UploadFileFromBase64String_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);

        var fileAsBytes = await localFile.ReadAllBytesAsync();
        var fileAsString64 = Convert.ToBase64String(fileAsBytes);

        // Act
        var result = await storageClient.UploadFile(fileAsString64, _uploadEndpoint, contentName);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task UploadLargeFile_WhenFileValid_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 50);
        var crc32 = Crc32Helper.CalculateFileCrc(localFile.FilePath);
        storageClient.SetChunkSize(4096000);

        // Act
        var result = await storageClient.UploadLargeFile(localFile.FileStream, _uploadLargeFile + "/upload", _uploadLargeFile + "/complete", null);

        // Assert
        result.IsSuccess
            .ShouldBeTrue();
        result.Value
            .ShouldBe(crc32);
    }

    [Theory]
    [Trait("Category", "LargeFile")]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task UploadFileFromStream_WhenFileIsLarge_ShouldRoundTrip(int gigabytes)
    {
        var storageClient = GetStorageClient();
        var downloadEndpoint = $"{ApiEndpoint}/download";
        var sizeBytes = LargeFileTestHelper.ResolveSizeBytes(gigabytes);

        await using var localFile = await LargeFileTestHelper.CreateRandomFileAsync(sizeBytes, ".bin");
        var expectedCrc = LargeFileTestHelper.CalculateFileCrc(localFile.FilePath);

        await using (var readStream = File.OpenRead(localFile.FilePath))
        {
            var uploadResult = await storageClient.UploadFile(readStream, _uploadEndpoint, "file", CancellationToken.None);
            uploadResult.IsSuccess.ShouldBeTrue();
            var metadata = uploadResult.Value ?? throw new InvalidOperationException("Upload did not return metadata");

            var downloadResult = await storageClient.DownloadFile(
                metadata.FullName ?? metadata.Name ?? localFile.Name,
                downloadEndpoint,
                cancellationToken: CancellationToken.None);
            downloadResult.IsSuccess.ShouldBeTrue();

            await using var downloaded = downloadResult.Value;
            var downloadedCrc = LargeFileTestHelper.CalculateFileCrc(downloaded.FilePath);
            downloadedCrc.ShouldBe(expectedCrc);

            await using var scope = TestApplication.Services.CreateAsyncScope();
            var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
            await storage.DeleteAsync(metadata.FullName ?? metadata.Name ?? localFile.Name, CancellationToken.None);
        }
    }

    [Theory]
    [Trait("Category", "LargeFile")]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task UploadLargeFile_WhenFileIsLarge_ReturnsExpectedChecksum(int gigabytes)
    {
        var storageClient = GetStorageClient();
        storageClient.SetChunkSize(8 * 1024 * 1024); // 8 MB chunks

        var sizeBytes = LargeFileTestHelper.ResolveSizeBytes(gigabytes);

        await using var localFile = await LargeFileTestHelper.CreateRandomFileAsync(sizeBytes, ".bin");
        var expectedCrc = LargeFileTestHelper.CalculateFileCrc(localFile.FilePath);

        var fileName = Path.GetFileName(localFile.FilePath);

        await using (var readStream = File.OpenRead(localFile.FilePath))
        {
            var result = await storageClient.UploadLargeFile(
                readStream,
                _uploadLargeFile + "/upload",
                _uploadLargeFile + "/complete",
                null,
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedCrc);
        }

        await using (var scope = TestApplication.Services.CreateAsyncScope())
        {
            var storage = scope.ServiceProvider.GetRequiredService<IStorage>();
            await storage.DeleteAsync(fileName, CancellationToken.None);
        }
    }
}
