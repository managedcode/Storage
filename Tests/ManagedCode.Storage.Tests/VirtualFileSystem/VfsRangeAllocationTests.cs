using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class VfsRangeAllocationTests
{
    [Theory]
    [InlineData(true, 8)]
    [InlineData(false, 8)]
    [InlineData(true, 10)]
    [InlineData(false, 10)]
    public async Task HugeRange_ReturnsOnlyBytesStillAvailable(bool seekable, int offset)
    {
        await using var session = await LocalVfsSession.CreateAsync(createStorage: seekable ? null : options => new ForwardOnlyStorage(options));
        var token = TestContext.Current.CancellationToken;
        var content = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var file = await session.FileSystem.GetFileAsync("/range.bin", token);
        await file.WriteAllBytesAsync(content, cancellationToken: token);

        (await file.ReadRangeAsync(offset, int.MaxValue, token)).ShouldBe(content[offset..]);
    }

    [Fact]
    public async Task ForwardOnlyRange_StopsAtRequestedCountAcrossBufferBoundaries()
    {
        await using var session = await LocalVfsSession.CreateAsync(createStorage: options => new ForwardOnlyStorage(options));
        var token = TestContext.Current.CancellationToken;
        var content = new byte[192 * 1024];
        new Random(17).NextBytes(content);
        var file = await session.FileSystem.GetFileAsync("/range.bin", token);
        await file.WriteAllBytesAsync(content, cancellationToken: token);

        const int offset = 13;
        const int count = 128 * 1024 + 11;
        (await file.ReadRangeAsync(offset, count, token)).ShouldBe(content[offset..(offset + count)]);
    }

    private sealed class ForwardOnlyStorage(FileSystemStorageOptions options) : FileSystemStorage(options)
    {
        public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var result = await base.GetStreamAsync(fileName, cancellationToken);
            if (result.IsFailed || result.Value is not FileStream file)
                return result;

            var path = file.Name;
            await file.DisposeAsync();
            return Result<Stream>.Succeed(new ForwardOnlyFileStream(path));
        }
    }

    private sealed class ForwardOnlyFileStream(string path) : FileStream(path, FileMode.Open, FileAccess.Read)
    {
        public override bool CanSeek => false;
    }
}
