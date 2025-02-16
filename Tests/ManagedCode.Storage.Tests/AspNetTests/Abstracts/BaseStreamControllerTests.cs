using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Constants;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Abstracts;

public abstract class BaseStreamControllerTests : BaseControllerTests
{
    private readonly string _streamEndpoint;
    private readonly string _uploadEndpoint;

    protected BaseStreamControllerTests(StorageTestApplication testApplication, string apiEndpoint) : base(testApplication, apiEndpoint)
    {
        _streamEndpoint = string.Format(ApiEndpoints.Base.StreamFile, ApiEndpoint);
        _uploadEndpoint = string.Format(ApiEndpoints.Base.UploadFile, ApiEndpoint);
    }

    [Fact]
    public async Task StreamFile_WhenFileExists_SaveToTempStorage_ReturnSuccess()
    {
        // Arrange
        var storageClient = GetStorageClient();
        var contentName = "file";
        var extension = ".txt";
        await using var localFile = LocalFile.FromRandomNameWithExtension(extension);
        FileHelper.GenerateLocalFile(localFile, 1);
        var fileCRC = Crc32Helper.Calculate(await localFile.ReadAllBytesAsync());
        var uploadFileBlob = await storageClient.UploadFile(localFile.FileStream, _uploadEndpoint, contentName);

        // Act
        var streamFileResult = await storageClient.GetFileStream(uploadFileBlob.Value.FullName, _streamEndpoint);

        // Assert
        streamFileResult.IsSuccess
            .Should()
            .BeTrue();
        streamFileResult.Should()
            .NotBeNull();

        await using var stream = streamFileResult.Value;
        await using var newLocalFile = await LocalFile.FromStreamAsync(stream, Path.GetTempPath(), Guid.NewGuid()
            .ToString("N") + extension);

        var streamedFileCRC = Crc32Helper.CalculateFileCrc(newLocalFile.FilePath);
        streamedFileCRC.Should()
            .Be(fileCRC);
    }

    [Fact]
    public async Task StreamFile_WhenFileDoNotExist_ReturnFail()
    {
        // Arrange
        var storageClient = GetStorageClient();

        // Act
        var streamFileResult = await storageClient.GetFileStream(Guid.NewGuid()
            .ToString(), _streamEndpoint);

        // Assert
        streamFileResult.IsFailed
            .Should()
            .BeTrue();
        streamFileResult.GetError()
            .Value
            .ErrorCode
            .Should()
            .Be(HttpStatusCode.InternalServerError.ToString());
    }
}