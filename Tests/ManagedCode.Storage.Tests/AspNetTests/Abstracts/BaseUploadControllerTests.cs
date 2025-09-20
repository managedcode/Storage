using System;
using System.Net;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Constants;
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
}
