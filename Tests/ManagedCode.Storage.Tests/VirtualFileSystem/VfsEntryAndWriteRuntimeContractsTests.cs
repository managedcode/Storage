using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.VirtualFileSystem.Implementations;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class VfsEntryAndWriteRuntimeContractsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EntryLookupAndSelectiveDeletion_ReflectTheRealStoredTree(bool cache)
    {
        await using var session = await LocalVfsSession.CreateAsync(cache);
        var token = TestContext.Current.CancellationToken;
        (await session.Storage.UploadAsync("direct", new UploadOptions { Directory = "tree", FileName = "direct.txt" }, token)).IsSuccess.ShouldBeTrue();
        (await session.Storage.UploadAsync("nested", new UploadOptions { Directory = "tree/child", FileName = "nested.txt" }, token)).IsSuccess.ShouldBeTrue();
        (await session.FileSystem.GetEntryAsync("/tree/direct.txt", token)).ShouldBeOfType<VirtualFile>();
        (await session.FileSystem.GetEntryAsync("/tree/", token)).ShouldBeOfType<VirtualDirectory>();
        (await session.FileSystem.GetEntryAsync("/missing", token)).ShouldBeNull();
        var direct = await session.FileSystem.DeleteDirectoryAsync("/tree", false, token);
        direct.Success.ShouldBeTrue();
        direct.FilesDeleted.ShouldBe(1);
        (await session.FileSystem.FileExistsAsync("/tree/child/nested.txt", token)).ShouldBeTrue();
        (await session.FileSystem.DirectoryExistsAsync("/tree/child", token)).ShouldBeTrue();
        var nested = await session.FileSystem.GetFileAsync("/tree/child/nested.txt", token);
        (await nested.ReadAllBytesAsync(token)).ShouldBe(Encoding.UTF8.GetBytes("nested"));
        var all = await session.FileSystem.DeleteDirectoryAsync("/tree", true, token);
        all.Success.ShouldBeTrue();
        all.FilesDeleted.ShouldBe(1);
        (await session.FileSystem.FileExistsAsync("/tree/child/nested.txt", token)).ShouldBeFalse();
        (await session.FileSystem.DirectoryExistsAsync("/tree/child", token)).ShouldBeFalse();
        var deleted = await session.FileSystem.GetFileAsync("/tree/child/nested.txt", token);
        await Should.ThrowAsync<VfsNotFoundException>(() => deleted.ReadAllBytesAsync(token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynchronousWriteDisposal_CommitsSeekedBytesAndReleasesTheBuffer(bool cache)
    {
        await using var session = await LocalVfsSession.CreateAsync(cache);
        var token = TestContext.Current.CancellationToken;
        (await session.Storage.UploadAsync("old", new UploadOptions("target.bin"), token)).IsSuccess.ShouldBeTrue();
        var file = await session.FileSystem.GetFileAsync("/target.bin", token);
        var stream = await file.OpenWriteAsync(new WriteOptions { Overwrite = true }, token);
        await Task.Run(() =>
        {
            stream.CanRead.ShouldBeFalse();
            stream.CanSeek.ShouldBeTrue();
            stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
            stream.Seek(1, SeekOrigin.Begin).ShouldBe(1);
            stream.Write(new byte[] { 8 }.AsSpan());
            stream.Position = 3;
            stream.SetLength(4);
            stream.Flush();
            Should.Throw<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
            stream.Dispose();
            stream.Dispose();
        }, token);
        stream.CanWrite.ShouldBeFalse();
        (await file.ReadAllBytesAsync(token)).ShouldBe(new byte[] { 1, 8, 3, 0 });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedWriteDisposal_PropagatesTheActualFilesystemFailure(bool asynchronous)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        (await session.Storage.UploadAsync("old", new UploadOptions("target.bin"), token)).IsSuccess.ShouldBeTrue();
        var file = await session.FileSystem.GetFileAsync("/target.bin", token);
        var stream = await file.OpenWriteAsync(new WriteOptions { Overwrite = true }, token);
        await stream.WriteAsync(Encoding.UTF8.GetBytes("cannot commit"), token);
        Directory.Delete(session.RootDirectory, recursive: true);
        await File.WriteAllTextAsync(session.RootDirectory, "an actual file blocks the storage directory", token);
        try
        {
            if (asynchronous) await Should.ThrowAsync<VfsOperationException>(() => stream.DisposeAsync().AsTask());
            else await Task.Run(() => Should.Throw<VfsOperationException>(() => stream.Dispose()), token);
            stream.CanWrite.ShouldBeFalse();
        }
        finally { File.Delete(session.RootDirectory); }
    }
}
