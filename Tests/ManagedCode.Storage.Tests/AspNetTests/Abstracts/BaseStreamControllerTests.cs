using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Shouldly;
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
        _streamEndpoint = $"{ApiEndpoint}/stream";
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
        FileHelper.GenerateLocalFileWithData(localFile, 100); // Generate file with actual data
        var fileCRC = Crc32Helper.CalculateFileCrc(localFile.FilePath); // Calculate CRC from file path
        await using var uploadStream = localFile.FileStream; // Get stream once
        var uploadFileBlob = await storageClient.UploadFile(uploadStream, _uploadEndpoint, contentName);
        uploadFileBlob.IsSuccess.ShouldBeTrue();
        var uploadedMetadata = uploadFileBlob.Value ?? throw new InvalidOperationException("Upload did not return metadata");

        // Act
        var streamFileResult = await storageClient.GetFileStream(uploadedMetadata.FullName, _streamEndpoint);

        // Assert
        streamFileResult.IsSuccess
            .ShouldBeTrue();
        var streamedValue = streamFileResult.Value ?? throw new InvalidOperationException("Stream result does not contain a stream");

        await using var stream = streamedValue;
        await using var newLocalFile = await LocalFile.FromStreamAsync(stream, Path.GetTempPath(), Guid.NewGuid()
            .ToString("N") + extension);

        var streamedFileCRC = Crc32Helper.CalculateFileCrc(newLocalFile.FilePath);
        streamedFileCRC.ShouldBe(fileCRC);
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
            .ShouldBeTrue();
        streamFileResult.Problem
            ?.StatusCode
            .ShouldBe((int)HttpStatusCode.InternalServerError);
    }
}
