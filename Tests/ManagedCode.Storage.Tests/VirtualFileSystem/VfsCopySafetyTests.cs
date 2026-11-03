using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class VfsCopySafetyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Copy_WithInterruptedSourceRead_PreservesDestination(bool directory, bool exists)
    {
        await using var session = await LocalVfsSession.CreateAsync(createStorage: options => new ObservedStorage(options));
        var token = TestContext.Current.CancellationToken;
        var source = await session.FileSystem.GetFileAsync("/source/file.txt", token);
        await source.WriteAllTextAsync("original source", cancellationToken: token);
        var destination = await session.FileSystem.GetFileAsync("/destination/file.txt", token);
        if (exists)
            await destination.WriteAllTextAsync("existing destination", cancellationToken: token);

        ((ObservedStorage)session.Storage).InterruptSourceRead = true;
        await Should.ThrowAsync<ObjectDisposedException>(() => session.FileSystem.CopyAsync(
            directory ? "/source" : "/source/file.txt",
            directory ? "/destination" : "/destination/file.txt",
            new CopyOptions { Overwrite = true }, cancellationToken: token));
        ((ObservedStorage)session.Storage).InterruptSourceRead = false;

        (await source.ReadAllTextAsync(cancellationToken: token)).ShouldBe("original source");
        (await destination.ExistsAsync(token)).ShouldBe(exists);
        if (exists)
            (await destination.ReadAllTextAsync(cancellationToken: token)).ShouldBe("existing destination");
    }

    [Fact]
    public async Task DirectoryCopy_WithoutProgress_ListsOnceAfterFindingSource()
    {
        await using var session = await LocalVfsSession.CreateAsync(createStorage: options => new ObservedStorage(options));
        var token = TestContext.Current.CancellationToken;
        var source = await session.FileSystem.GetFileAsync("/source/file.txt", token);
        await source.WriteAllTextAsync("copy bytes", cancellationToken: token);
        (await session.FileSystem.DirectoryExistsAsync("/source", token)).ShouldBeTrue();
        var storage = (ObservedStorage)session.Storage;
        storage.ListRequests = 0;

        await session.FileSystem.CopyAsync("/source", "/destination", cancellationToken: token);

        storage.ListRequests.ShouldBe(1);
        var destination = await session.FileSystem.GetFileAsync("/destination/file.txt", token);
        (await destination.ReadAllTextAsync(cancellationToken: token)).ShouldBe("copy bytes");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Copy_WithProgress_ReportsActualSize(bool directory)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var source = await session.FileSystem.GetFileAsync("/source/file.bin", token);
        var content = new byte[] { 7, 11, 19, 23 };
        await source.WriteAllBytesAsync(content, cancellationToken: token);
        var progress = new RecordedProgress();

        await session.FileSystem.CopyAsync(directory ? "/source" : "/source/file.bin",
            directory ? "/destination" : "/destination/file.bin", progress: progress, cancellationToken: token);

        progress.Reports[0].TotalBytes.ShouldBe(content.LongLength);
        progress.Reports[^1].CopiedBytes.ShouldBe(content.LongLength);
        progress.Reports[^1].CopiedFiles.ShouldBe(1);
    }

    [Theory]
    [InlineData("/source/file.txt", "/source/./file.txt")]
    [InlineData("/source", "/source/child")]
    public async Task Copy_IntoItself_RejectsBeforeWriting(string source, string destination)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var original = await session.FileSystem.GetFileAsync("/source/file.txt", token);
        await original.WriteAllTextAsync("original", cancellationToken: token);

        await Should.ThrowAsync<VfsOperationException>(() => session.FileSystem.CopyAsync(source, destination,
            new CopyOptions { Overwrite = true }, cancellationToken: token));

        (await original.ReadAllTextAsync(cancellationToken: token)).ShouldBe("original");
        Directory.GetFiles(session.RootDirectory, "*", SearchOption.AllDirectories).Length.ShouldBe(1);
    }

    private sealed class RecordedProgress : IProgress<CopyProgress>
    {
        public List<CopyProgress> Reports { get; } = [];
        public void Report(CopyProgress value) => Reports.Add(value);
    }

    private sealed class ObservedStorage(FileSystemStorageOptions options) : FileSystemStorage(options)
    {
        public bool InterruptSourceRead { get; set; }
        public int ListRequests { get; set; }

        public override IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
            CancellationToken cancellationToken = default)
        {
            ListRequests++;
            return base.GetBlobMetadataListAsync(directory, cancellationToken);
        }

        public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var result = await base.GetStreamAsync(fileName, cancellationToken);
            if (InterruptSourceRead && fileName == "source/file.txt" && result.IsSuccess && result.Value is FileStream file)
            {
                await file.DisposeAsync();
                return Result<Stream>.Succeed(new InterruptedFileReadStream(file.Name));
            }

            return result;
        }
    }

    private sealed class InterruptedFileReadStream(string path) : FileStream(path, FileMode.Open, FileAccess.Read)
    {
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var prefix = new byte[4];
            await ReadExactlyAsync(prefix, cancellationToken);
            await destination.WriteAsync(prefix, cancellationToken);
            await DisposeAsync();
            await ReadExactlyAsync(prefix, cancellationToken);
        }
    }
}
