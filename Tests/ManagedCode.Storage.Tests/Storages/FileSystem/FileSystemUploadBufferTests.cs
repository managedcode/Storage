using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Tests.VirtualFileSystem;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.FileSystem;

public sealed class FileSystemUploadBufferTests
{
    [Fact]
    public async Task NonSeekableUpload_BoundsReadBuffersAndPreservesTheCallerStream()
    {
        await using var session = await LocalVfsSession.CreateAsync();
        var token = TestContext.Current.CancellationToken;
        var sourcePath = Path.Combine(session.RootDirectory, "source.bin");
        var destinationPath = Path.Combine(session.RootDirectory, "destination.bin");
        var bytes = new byte[512 * 1024 + 17];
        new Random(19).NextBytes(bytes);
        await File.WriteAllBytesAsync(sourcePath, bytes, token);
        using var observed = new ObservedReadStream(File.OpenRead(sourcePath));

        var result = await session.Storage.UploadAsync(observed, new UploadOptions("destination.bin"), token);

        result.IsSuccess.ShouldBeTrue();
        observed.MaxRequestedBytes.ShouldBeGreaterThan(0);
        observed.MaxRequestedBytes.ShouldBeLessThanOrEqualTo(256 * 1024);
        observed.CanRead.ShouldBeTrue();
        (await File.ReadAllBytesAsync(destinationPath, token)).ShouldBe(bytes);
    }

    private sealed class ObservedReadStream(FileStream source) : Stream
    {
        public int MaxRequestedBytes { get; private set; }
        public override bool CanRead => source.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            MaxRequestedBytes = Math.Max(MaxRequestedBytes, count);
            return source.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            MaxRequestedBytes = Math.Max(MaxRequestedBytes, buffer.Length);
            return source.ReadAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                source.Dispose();

            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
