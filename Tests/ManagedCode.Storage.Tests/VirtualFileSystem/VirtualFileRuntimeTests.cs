using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class VirtualFileRuntimeTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteStream_PersistsSyncAndAsyncWritesAndEnforcesLifetime(bool cache)
    {
        await using var session = await LocalVfsSession.CreateAsync(cache);
        var token = TestContext.Current.CancellationToken;
        var file = await session.FileSystem.GetFileAsync(new VfsPath("/contract.bin"), token);
        var stream = await file.OpenWriteAsync(cancellationToken: token);
        stream.CanRead.ShouldBeFalse();
        stream.CanSeek.ShouldBeTrue();
        stream.CanWrite.ShouldBeTrue();
        stream.Write(new byte[] { 1, 2 }, 0, 2);
        stream.Write(new byte[] { 3 }.AsSpan());
        await stream.WriteAsync(new byte[] { 4 }, 0, 1, token);
        await stream.WriteAsync(new byte[] { 5 }.AsMemory(), token);
        stream.Position.ShouldBe(5);
        stream.Length.ShouldBe(5);
        stream.Seek(0, SeekOrigin.Begin).ShouldBe(0);
        stream.SetLength(4);
        stream.Flush();
        await stream.FlushAsync(token);
        Should.Throw<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        await stream.DisposeAsync();
        await stream.DisposeAsync();
        stream.CanWrite.ShouldBeFalse();
        Should.Throw<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
        (await file.ReadAllBytesAsync(token)).ShouldBe(new byte[] { 1, 2, 3, 4 });
        await file.RefreshAsync(token);
        file.Size.ShouldBe(4);
        file.Type.ShouldBe(VfsEntryType.File);
        (await file.GetParentAsync(token)).Name.ShouldBe("/");
        var metadata = await file.GetMetadataAsync(token);
        (await file.GetMetadataAsync(token)).ShouldBe(metadata);
        await Should.ThrowAsync<VfsAlreadyExistsException>(() => file.OpenWriteAsync(new WriteOptions { Overwrite = false }, token));
        await Should.ThrowAsync<VfsConcurrencyException>(() => file.OpenWriteAsync(new WriteOptions { ExpectedETag = "stale" }, token));
        await Should.ThrowAsync<VfsConcurrencyException>(() => file.SetMetadataAsync(new System.Collections.Generic.Dictionary<string, string>(), "stale", token));
        await Should.ThrowAsync<VfsNotSupportedException>(() => file.StartMultipartUploadAsync(token));
        (await file.DeleteAsync(token)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 3, "012")]
    [InlineData(3, 4, "3456")]
    [InlineData(8, 4, "89")]
    [InlineData(20, 4, "")]
    public async Task RangeRead_ReturnsBytesFromRequestedOffset(long offset, int count, string expected)
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var file = await session.FileSystem.GetFileAsync(new VfsPath("/range.txt"), token);
        await file.WriteAllTextAsync("0123456789", cancellationToken: token);
        Encoding.UTF8.GetString(await file.ReadRangeAsync(offset, count, token)).ShouldBe(expected);
    }

    [Fact]
    public async Task MissingAndFailedWrites_ExposeErrorsWithoutCreatingFiles()
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var missing = await session.FileSystem.GetFileAsync(new VfsPath("/missing"), token);
        await Should.ThrowAsync<VfsNotFoundException>(() => missing.OpenReadAsync(cancellationToken: token));
        await Should.ThrowAsync<VfsNotFoundException>(() => missing.ReadAllBytesAsync(token));
        var parentFile = await session.FileSystem.GetFileAsync(new VfsPath("/obstruction"), token);
        await parentFile.WriteAllTextAsync("parent is a file", cancellationToken: token);
        var blocked = await session.FileSystem.GetFileAsync(new VfsPath("/obstruction/child"), token);
        await Should.ThrowAsync<VfsOperationException>(() => blocked.WriteAllTextAsync("cannot persist", cancellationToken: token));
        (await blocked.ExistsAsync(token)).ShouldBeFalse();
    }
}
