using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Server.ChunkUpload;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Server;

public sealed class ChunkUploadLengthBudgetTests : IDisposable
{
    private const string UploadId = "bounded-upload";
    private const string FileName = "object.bin";
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("storage-chunk-budget-");

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task FirstChunk_ExceedingDeclaredSize_DoesNotReserveAWorkingDirectory(long size)
    {
        var options = new ChunkUploadOptions { TempPath = Path.Combine(_root.FullName, "chunks"), MaxActiveSessions = 1 };
        var service = new ChunkUploadService(options);

        var rejected = await AppendAsync(service, [1, 2, 3], size: size);

        rejected.IsFailed.ShouldBeTrue();
        rejected.Problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        Directory.GetDirectories(options.TempPath).ShouldBeEmpty();
        (await AppendAsync(service, [1, 2, 3])).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OversizedAppendOrReplacement_PreservesPreviouslyAcceptedBytes(bool replacement)
    {
        var service = CreateService();
        (await AppendAsync(service, [1, 2, 3])).IsSuccess.ShouldBeTrue();
        if (replacement)
            (await AppendAsync(service, [4, 5], index: 2)).IsSuccess.ShouldBeTrue();

        var rejected = replacement
            ? await AppendAsync(service, [9, 8, 7, 6])
            : await AppendAsync(service, [6, 7, 8], index: 2);

        rejected.IsFailed.ShouldBeTrue();
        rejected.Problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        if (!replacement)
            (await AppendAsync(service, [4, 5], index: 2)).IsSuccess.ShouldBeTrue();
        (await CompleteAndReadAsync(service)).ShouldBe(new byte[] { 1, 2, 3, 4, 5 });
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    public async Task UnknownLength_RemainsCompatibleWithBothDescriptorForms(long? size)
    {
        var service = CreateService();
        (await AppendAsync(service, [4, 7, 9], total: 0, size: size)).IsSuccess.ShouldBeTrue();
        (await CompleteAndReadAsync(service)).ShouldBe(new byte[] { 4, 7, 9 });
    }

    [Fact]
    public async Task ShortFormStream_PreservesAcceptedChunk()
    {
        var service = CreateService();
        (await AppendAsync(service, [1, 2, 3])).IsSuccess.ShouldBeTrue();
        (await AppendAsync(service, [4, 5], index: 2)).IsSuccess.ShouldBeTrue();

        await Should.ThrowAsync<InvalidDataException>(() => AppendAsync(service, [9, 8], formLength: 3));

        (await CompleteAndReadAsync(service)).ShouldBe(new byte[] { 1, 2, 3, 4, 5 });
    }

    private ChunkUploadService CreateService() =>
        new(new ChunkUploadOptions { TempPath = Path.Combine(_root.FullName, "chunks") });

    private async Task<byte[]> CompleteAndReadAsync(ChunkUploadService service)
    {
        var token = TestContext.Current.CancellationToken;
        var destination = Path.Combine(_root.FullName, "objects");
        using var storage = new FileSystemStorage(new FileSystemStorageOptions { BaseFolder = destination });
        var completed = await service.CompleteAsync(new ChunkUploadCompleteRequest { UploadId = UploadId }, storage, token);
        completed.IsSuccess.ShouldBeTrue();
        return await File.ReadAllBytesAsync(Path.Combine(destination, FileName), token);
    }

    private static async Task<Result> AppendAsync(ChunkUploadService service, byte[] bytes, int index = 1,
        int total = 2, long? size = 5, int? formLength = null)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        return await service.AppendChunkAsync(new FileUploadPayload
        {
            File = new FormFile(stream, 0, formLength ?? bytes.Length, "file", FileName),
            Payload = new FilePayload
            {
                UploadId = UploadId,
                FileName = FileName,
                ChunkIndex = index,
                TotalChunks = total,
                FileSize = size,
                ChunkSize = formLength ?? bytes.Length
            }
        }, TestContext.Current.CancellationToken);
    }

    public void Dispose() => _root.Delete(recursive: true);
}
