using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Client.SignalR.Models;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.AspNetTests.Abstracts;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Constants;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Microsoft.AspNetCore.SignalR;
using Xunit.Abstractions;

namespace ManagedCode.Storage.Tests.AspNetTests.Azure;

public class AzureSignalRStorageTests : BaseSignalRStorageTests
{
    private readonly ITestOutputHelper _output;

    public AzureSignalRStorageTests(StorageTestApplication testApplication, ITestOutputHelper output)
        : base(testApplication, ApiEndpoints.Azure)
    {
        _output = output;
    }

    [Fact]
    public async Task UploadStreamAsync_WhenFileProvided_ShouldStoreBlob()
    {
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);

        await using var uploadStream = File.OpenRead(localFile.FilePath);
        var descriptor = CreateDescriptor(Path.GetFileName(localFile.FilePath), MimeTypes.MimeHelper.TEXT, uploadStream.Length);

        await using var scope = TestApplication.Services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IAzureStorage>();
        await storage.CreateContainerAsync(CancellationToken.None);

        await using var client = CreateClient();
        await client.ConnectAsync(CancellationToken.None);

        StorageTransferStatus? lastProgress = null;
        StorageTransferStatus? faultStatus = null;

        client.TransferProgress += (_, status) =>
        {
            if (status.TransferId == descriptor.TransferId)
            {
                lastProgress = status;
            }
        };

        client.TransferCompleted += (_, status) =>
        {
            if (status.TransferId == descriptor.TransferId)
            {
                lastProgress = status;
            }
        };

        client.TransferFaulted += (_, status) => faultStatus = status;

        StorageTransferStatus status;
        try
        {
            status = await client.UploadAsync(uploadStream, descriptor, cancellationToken: CancellationToken.None);
        }
        catch (HubException ex)
        {
            _output.WriteLine(ex.ToString());
            if (ex.InnerException is not null)
            {
                _output.WriteLine($"Inner: {ex.InnerException}");
            }
            var message = $"SignalR upload failed: {ex.Message}; fault status: {faultStatus?.Error}; detail: {ex}; inner: {ex.InnerException}";
            throw new Xunit.Sdk.XunitException(message);
        }

        status.ShouldNotBeNull();
        status.IsCompleted.ShouldBeTrue();
        status.Metadata.ShouldNotBeNull();

        var exists = await storage.ExistsAsync(status.Metadata!.FullName ?? status.Metadata.Name ?? descriptor.FileName);
        exists.IsSuccess.ShouldBeTrue();
        exists.Value.ShouldBeTrue();

        lastProgress.ShouldNotBeNull();
        lastProgress!.IsCompleted.ShouldBeTrue();
        lastProgress.BytesTransferred.ShouldBeGreaterThan(0);

        await storage.DeleteAsync(status.Metadata.FullName ?? status.Metadata.Name ?? descriptor.FileName);
        await client.DisconnectAsync();
    }

    [Fact]
    public async Task DownloadStreamAsync_WhenBlobExists_ShouldDownloadContent()
    {
        await using var scope = TestApplication.Services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IAzureStorage>();

        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);

        var uploadResult = await storage.UploadAsync(localFile.FileInfo, new UploadOptions(localFile.FileInfo.Name), CancellationToken.None);
        uploadResult.IsSuccess.ShouldBeTrue();

        await using var client = CreateClient();
        await client.ConnectAsync(CancellationToken.None);

        await using var memory = new MemoryStream();
        var status = await client.DownloadAsync(localFile.FileInfo.Name, memory, cancellationToken: CancellationToken.None);

        status.ShouldNotBeNull();
        status.IsCompleted.ShouldBeTrue();
        status.BytesTransferred.ShouldBe(memory.Length);

        var expectedCrc = Crc32Helper.CalculateFileCrc(localFile.FilePath);
        memory.Position = 0;
        await using var downloadedFile = await LocalFile.FromStreamAsync(memory, Environment.CurrentDirectory, Guid.NewGuid().ToString("N") + localFile.FileInfo.Extension);
        var downloadedCrc = Crc32Helper.CalculateFileCrc(downloadedFile.FilePath);
        downloadedCrc.ShouldBe(expectedCrc);

        await storage.DeleteAsync(localFile.FileInfo.Name);
        await client.DisconnectAsync();
    }

    [Theory]
    [Trait("Category", "LargeFile")]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task UploadStreamAsync_WhenFileIsLarge_ShouldRoundTrip(int gigabytes)
    {
        var sizeBytes = LargeFileTestHelper.ResolveSizeBytes(gigabytes);

        await using var localFile = await LargeFileTestHelper.CreateRandomFileAsync(sizeBytes, ".bin");
        var expectedCrc = LargeFileTestHelper.CalculateFileCrc(localFile.FilePath);

        var descriptor = CreateDescriptor(Path.GetFileName(localFile.FilePath), "application/octet-stream", sizeBytes);

        await using var scope = TestApplication.Services.CreateAsyncScope();
        var storage = scope.ServiceProvider.GetRequiredService<IAzureStorage>();
        await storage.CreateContainerAsync(CancellationToken.None);

        await using var client = CreateClient();
        await client.ConnectAsync(CancellationToken.None);

        StorageTransferStatus status;
        await using (var readStream = File.OpenRead(localFile.FilePath))
        {
            status = await client.UploadAsync(readStream, descriptor, cancellationToken: CancellationToken.None);
        }

        status.IsCompleted.ShouldBeTrue();
        status.Metadata.ShouldNotBeNull();

        var remoteName = status.Metadata!.FullName ?? status.Metadata.Name ?? descriptor.FileName;

        var downloadPath = Path.Combine(Environment.CurrentDirectory, "large-file-tests", $"download-{Guid.NewGuid():N}.bin");
        await using var downloadedFile = new LocalFile(downloadPath);
        await using (var destination = File.Open(downloadedFile.FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            destination.SetLength(0);
            destination.Position = 0;
            var downloadStatus = await client.DownloadAsync(remoteName, destination, cancellationToken: CancellationToken.None);
            downloadStatus.IsCompleted.ShouldBeTrue();
        }

        var downloadedCrc = LargeFileTestHelper.CalculateFileCrc(downloadedFile.FilePath);
        downloadedCrc.ShouldBe(expectedCrc);

        await storage.DeleteAsync(remoteName, CancellationToken.None);
        await client.DisconnectAsync();
    }
}
