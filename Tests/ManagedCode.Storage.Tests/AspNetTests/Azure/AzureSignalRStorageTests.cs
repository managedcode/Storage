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

namespace ManagedCode.Storage.Tests.AspNetTests.Azure;

public class AzureSignalRStorageTests : BaseSignalRStorageTests
{
    public AzureSignalRStorageTests(StorageTestApplication testApplication)
        : base(testApplication, ApiEndpoints.Azure)
    {
    }

    [Fact]
    public async Task UploadStreamAsync_WhenFileProvided_ShouldStoreBlob()
    {
        await using var localFile = LocalFile.FromRandomNameWithExtension(".txt");
        FileHelper.GenerateLocalFile(localFile, 1);

        await using var uploadStream = File.OpenRead(localFile.FilePath);
        var descriptor = CreateDescriptor(Path.GetFileName(localFile.FilePath), "text/plain", uploadStream.Length);

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

        client.TransferFaulted += (_, status) => faultStatus = status;

        StorageTransferStatus status;
        try
        {
            status = await client.UploadAsync(uploadStream, descriptor, cancellationToken: CancellationToken.None);
        }
        catch (HubException ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine($"Inner: {ex.InnerException}");
            throw new Xunit.Sdk.XunitException($"SignalR upload failed: {ex.Message}; fault status: {faultStatus?.Error}; detail: {ex}; inner: {ex.InnerException}");
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
        await using var downloadedFile = await LocalFile.FromStreamAsync(memory, Path.GetTempPath(), Guid.NewGuid().ToString("N") + localFile.FileInfo.Extension);
        var downloadedCrc = Crc32Helper.CalculateFileCrc(downloadedFile.FilePath);
        downloadedCrc.ShouldBe(expectedCrc);

        await storage.DeleteAsync(localFile.FileInfo.Name);
        await client.DisconnectAsync();
    }
}
