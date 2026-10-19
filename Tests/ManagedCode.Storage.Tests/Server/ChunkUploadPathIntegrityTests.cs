using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Server.ChunkUpload;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Server;

public sealed class ChunkUploadPathIntegrityTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Environment.CurrentDirectory, "managedcode-chunk-tests", Guid.NewGuid().ToString("N"));

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletionDoesNotOverwriteFilesOutsideItsSession(bool absolute)
    {
        var chunks = Path.Combine(_root, "chunks");
        Directory.CreateDirectory(chunks);
        var outside = Path.Combine(chunks, "existing.bin");
        byte[] original = [9, 8, 7];
        await File.WriteAllBytesAsync(outside, original, TestContext.Current.CancellationToken);
        var name = absolute ? outside : "../existing.bin";
        var service = new ChunkUploadService(new ChunkUploadOptions { TempPath = chunks });
        using var storage = CreateStorage();
        await AppendAsync(service, "upload", name);

        var result = await service.CompleteAsync(new ChunkUploadCompleteRequest
        {
            UploadId = "upload",
            CommitToStorage = false
        }, storage, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        File.Exists(outside).ShouldBeTrue();
        (await File.ReadAllBytesAsync(outside, TestContext.Current.CancellationToken)).ShouldBe(original);
    }

    [Fact]
    public async Task FileNameCannotTruncateAnUploadedPartDuringMerge()
    {
        var service = new ChunkUploadService(new ChunkUploadOptions { TempPath = Path.Combine(_root, "chunks") });
        using var storage = CreateStorage();
        await AppendAsync(service, "upload", "000001.part");
        var result = await service.CompleteAsync(new ChunkUploadCompleteRequest { UploadId = "upload" }, storage,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var downloaded = await storage.GetStreamAsync("000001.part", TestContext.Current.CancellationToken);
        downloaded.IsSuccess.ShouldBeTrue();
        await using var content = downloaded.Value!;
        using var bytes = new MemoryStream();
        await content.CopyToAsync(bytes, TestContext.Current.CancellationToken);
        bytes.ToArray().ShouldBe(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task RejectedUploadIdentityDoesNotAliasAnExistingSession()
    {
        var service = new ChunkUploadService(new ChunkUploadOptions { TempPath = Path.Combine(_root, "chunks") });
        using var storage = CreateStorage();
        await AppendAsync(service, "ab", "healthy.bin");
        await Should.ThrowAsync<ArgumentException>(() => AppendAsync(service, "a/b", "other.bin"));
        var completed = await service.CompleteAsync(new ChunkUploadCompleteRequest { UploadId = "ab" }, storage,
            TestContext.Current.CancellationToken);
        completed.IsSuccess.ShouldBeTrue();
        var exists = await storage.ExistsAsync("healthy.bin", TestContext.Current.CancellationToken);
        exists.IsSuccess.ShouldBeTrue();
        exists.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task CompletionRetainsMergedContentOnlyWhenRequested()
    {
        var chunks = Path.Combine(_root, "chunks");
        var service = new ChunkUploadService(new ChunkUploadOptions { TempPath = chunks });
        using var storage = CreateStorage();
        await AppendAsync(service, "retained", "retained.bin");
        var completed = await service.CompleteAsync(new ChunkUploadCompleteRequest
        {
            UploadId = "retained",
            CommitToStorage = false,
            KeepMergedFile = true
        }, storage, TestContext.Current.CancellationToken);
        completed.IsSuccess.ShouldBeTrue();
        var files = Directory.GetFiles(chunks, "*.bin", SearchOption.AllDirectories);
        files.Length.ShouldBe(1);
        (await File.ReadAllBytesAsync(files[0], TestContext.Current.CancellationToken)).ShouldBe(new byte[] { 1, 2, 3 });
        Directory.GetFiles(chunks, "*.part", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    private FileSystemStorage CreateStorage() => new(new FileSystemStorageOptions
    {
        BaseFolder = Path.Combine(_root, "storage"),
        CreateContainerIfNotExists = true
    });

    private static async Task AppendAsync(ChunkUploadService service, string id, string name)
    {
        using var content = new MemoryStream(new byte[] { 1, 2, 3 }, writable: false);
        var result = await service.AppendChunkAsync(new FileUploadPayload
        {
            File = new FormFile(content, 0, content.Length, "file", name),
            Payload = new FilePayload
            {
                UploadId = id,
                FileName = name,
                FileSize = content.Length,
                ChunkIndex = 1,
                TotalChunks = 1,
                ChunkSize = 3
            }
        }, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }
}
