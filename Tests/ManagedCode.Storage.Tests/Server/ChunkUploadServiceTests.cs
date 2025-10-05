using System;
using System.IO;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Server.ChunkUpload;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace ManagedCode.Storage.Tests.Server;

public class ChunkUploadServiceTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Environment.CurrentDirectory, "managedcode-chunk-tests", Guid.NewGuid().ToString());
    private ChunkUploadOptions _options = null!;

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _options = new ChunkUploadOptions
        {
            TempPath = Path.Combine(_root, "chunks"),
            SessionTtl = TimeSpan.FromMinutes(10),
            MaxActiveSessions = 4
        };
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task CompleteAsync_WithCommit_ShouldMergeChunksAndUpload()
    {
        using var storage = CreateStorage();
        await storage.CreateContainerAsync();

        var service = new ChunkUploadService(_options);
        var uploadId = Guid.NewGuid().ToString("N");
        var fileName = "video.bin";

        var payload = new byte[5 * 1024];
        new Random(42).NextBytes(payload);
        var checksum = Crc32Helper.Calculate(payload);

        var chunkSize = 2048;
        var totalChunks = (int)Math.Ceiling(payload.Length / (double)chunkSize);

        for (var i = 0; i < totalChunks; i++)
        {
            var sliceLength = Math.Min(chunkSize, payload.Length - (i * chunkSize));
            var slice = new byte[sliceLength];
            Array.Copy(payload, i * chunkSize, slice, 0, sliceLength);

            var formFile = CreateFormFile(slice, fileName);

            var appendResult = await service.AppendChunkAsync(new FileUploadPayload
            {
                File = formFile,
                Payload = new FilePayload
                {
                    UploadId = uploadId,
                    FileName = fileName,
                    ContentType = "application/octet-stream",
                    ChunkIndex = i + 1,
                    ChunkSize = sliceLength,
                    TotalChunks = totalChunks,
                    FileSize = payload.Length
                }
            }, default);

            appendResult.IsSuccess.ShouldBeTrue();
        }

        var completeResult = await service.CompleteAsync(new ChunkUploadCompleteRequest
        {
            UploadId = uploadId,
            FileName = fileName,
            CommitToStorage = true
        }, storage, default);

        completeResult.IsSuccess.ShouldBeTrue();
        var completion = completeResult.Value ?? throw new InvalidOperationException("Completion result is null");
        completion.Checksum.ShouldBe(checksum);
        completion.Metadata.ShouldNotBeNull();

        var metadata = await storage.GetBlobMetadataAsync(fileName);
        metadata.IsSuccess.ShouldBeTrue();
        (metadata.Value ?? throw new InvalidOperationException("Metadata value is null")).Length.ShouldBe((ulong)payload.Length);

        var download = await storage.DownloadAsync(fileName);
        download.IsSuccess.ShouldBeTrue();
        var downloadedFile = download.Value ?? throw new InvalidOperationException("Download returned null file");
        using var ms = new MemoryStream();
        await downloadedFile.FileStream.CopyToAsync(ms);
        ms.ToArray().ShouldBe(payload);

        var repeat = await service.CompleteAsync(new ChunkUploadCompleteRequest { UploadId = uploadId }, storage, default);
        repeat.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Abort_ShouldRemoveSessionArtifacts()
    {
        var service = new ChunkUploadService(_options);
        var uploadId = Guid.NewGuid().ToString("N");
        var fileName = "artifact.bin";
        var chunkBytes = new byte[] { 1, 2, 3, 4 };
        var formFile = CreateFormFile(chunkBytes, fileName);

        var append = await service.AppendChunkAsync(new FileUploadPayload
        {
            File = formFile,
            Payload = new FilePayload
            {
                UploadId = uploadId,
                FileName = fileName,
                ChunkIndex = 1,
                ChunkSize = chunkBytes.Length,
                TotalChunks = 1
            }
        }, default);

        append.IsSuccess.ShouldBeTrue();
        var workingDirectory = Path.Combine(_options.TempPath, uploadId);
        Directory.Exists(workingDirectory).ShouldBeTrue();

        service.Abort(uploadId);
        Directory.Exists(workingDirectory).ShouldBeFalse();
    }

    [Fact]
    public async Task AppendChunk_WhenSessionLimitExceeded_ShouldFail()
    {
        var options = new ChunkUploadOptions
        {
            TempPath = Path.Combine(_root, "limited"),
            SessionTtl = TimeSpan.FromMinutes(10),
            MaxActiveSessions = 1
        };

        var service = new ChunkUploadService(options);

        async Task<bool> Append(string uploadId)
        {
            var formFile = CreateFormFile(new byte[] { 1 }, "chunk.bin");
            var result = await service.AppendChunkAsync(new FileUploadPayload
            {
                File = formFile,
                Payload = new FilePayload
                {
                    UploadId = uploadId,
                    FileName = "chunk.bin",
                    ChunkIndex = 1,
                    ChunkSize = 1,
                    TotalChunks = 1
                }
            }, default);

            return result.IsSuccess;
        }

        (await Append("upload-a")).ShouldBeTrue();
        (await Append("upload-b")).ShouldBeFalse();

        service.Abort("upload-a");
        service.Abort("upload-b");
    }

    [Fact]
    public async Task CompleteAsync_WithLargeChunkSize_ShouldPreserveChecksum()
    {
        using var storage = CreateStorage();
        await storage.CreateContainerAsync();

        var service = new ChunkUploadService(_options);
        var uploadId = Guid.NewGuid().ToString("N");
        var fileName = "single-chunk.bin";

        var payload = new byte[51];
        new Random(123).NextBytes(payload);
        var checksum = Crc32Helper.Calculate(payload);

        var formFile = CreateFormFile(payload, fileName);

        var appendResult = await service.AppendChunkAsync(new FileUploadPayload
        {
            File = formFile,
            Payload = new FilePayload
            {
                UploadId = uploadId,
                FileName = fileName,
                ContentType = "application/octet-stream",
                ChunkIndex = 1,
                ChunkSize = 4_096_000,
                TotalChunks = 1,
                FileSize = payload.Length
            }
        }, default);

        appendResult.IsSuccess.ShouldBeTrue();

        var complete = await service.CompleteAsync(new ChunkUploadCompleteRequest
        {
            UploadId = uploadId,
            FileName = fileName,
            CommitToStorage = true
        }, storage, default);

        complete.IsSuccess.ShouldBeTrue();
        (complete.Value ?? throw new InvalidOperationException("Completion result is null")).Checksum.ShouldBe(checksum);
    }

    private FileSystemStorage CreateStorage()
    {
        var baseFolder = Path.Combine(_root, "storage");
        return new FileSystemStorage(new FileSystemStorageOptions
        {
            BaseFolder = baseFolder,
            CreateContainerIfNotExists = true
        });
    }

    private static FormFile CreateFormFile(byte[] bytes, string fileName)
    {
        var stream = new MemoryStream(bytes);
        var formFile = new FormFile(stream, 0, bytes.Length, "File", fileName)
        {
            Headers = new HeaderDictionary
            {
                { "Content-Type", new StringValues("application/octet-stream") }
            }
        };
        formFile.ContentType = "application/octet-stream";
        return formFile;
    }
}
