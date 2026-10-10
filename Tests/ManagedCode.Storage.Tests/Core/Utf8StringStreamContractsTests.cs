using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using ManagedCode.Storage.Core;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

public sealed class Utf8StringStreamContractsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("ASCII")]
    [InlineData("Привіт 🌍")]
    public async Task PooledAndEncodedStreams_RoundTripExactUtf8Bytes(string content)
    {
        using var stream = Utf8StringStream.CreatePooled(content, ArrayPool<byte>.Shared);
        var expected = Encoding.UTF8.GetBytes(content);
        stream.Length.ShouldBe(expected.Length);
        stream.CanRead.ShouldBeTrue();
        stream.CanSeek.ShouldBeTrue();
        stream.CanWrite.ShouldBeFalse();
        stream.GetUtf8Bytes().ToArray().ShouldBe(expected);
        stream.GetUtf8Span().ToArray().ShouldBe(expected);
        stream.ToString().ShouldBe(content);
        var actual = new byte[expected.Length + 2];
        (await stream.ReadAsync(actual.AsMemory(1, expected.Length), TestContext.Current.CancellationToken)).ShouldBe(expected.Length);
        actual.Skip(1).Take(expected.Length).ShouldBe(expected);
        stream.Read(actual.AsSpan()).ShouldBe(0);
        stream.ReadByte().ShouldBe(-1);
        stream.ToStringFromPosition().ShouldBeEmpty();
        await stream.FlushAsync(TestContext.Current.CancellationToken);
        stream.Flush();
    }

    [Fact]
    public void SeekAndByteReads_TrackTheUnreadContent()
    {
        using var stream = new Utf8StringStream("abc");
        stream.Seek(1, SeekOrigin.Begin).ShouldBe(1);
        stream.ToStringFromPosition().ShouldBe("bc");
        stream.ReadByte().ShouldBe((int)'b');
        stream.Seek(-1, SeekOrigin.Current).ShouldBe(1);
        stream.Seek(-1, SeekOrigin.End).ShouldBe(2);
        stream.Position = 0;
        var bytes = new byte[5];
        stream.Read(bytes, 1, 3).ShouldBe(3);
        bytes.ShouldBe(new byte[] { 0, 97, 98, 99, 0 });
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Position = -1);
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Position = 4);
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Seek(-4, SeekOrigin.End));
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Seek(1, SeekOrigin.End));
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Seek(0, (SeekOrigin)99));
    }

    [Fact]
    public async Task InvalidReadAndWriteRequests_RejectWithoutChangingPosition()
    {
        using var stream = new Utf8StringStream(new ReadOnlyMemory<byte>(new byte[] { 1, 2 }));
        Should.Throw<ArgumentNullException>(() => stream.Read(null!, 0, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Read(new byte[2], -1, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Read(new byte[2], 0, -1));
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Read(new byte[2], 3, 0));
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Read(new byte[2], 1, 2));
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            (await stream.ReadAsync(new byte[1], new CancellationToken(true))).ShouldBe(0);
        });
        Should.Throw<NotSupportedException>(() => stream.SetLength(0));
        Should.Throw<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
        Should.Throw<NotSupportedException>(() => stream.Write(new byte[1].AsSpan()));
        await Should.ThrowAsync<NotSupportedException>(async () => await stream.WriteAsync(new byte[1]));
        Should.Throw<NotSupportedException>(() => stream.WriteByte(1));
        Should.Throw<ArgumentNullException>(() => new Utf8StringStream((string)null!));
        Should.Throw<ArgumentNullException>(() => Utf8StringStream.CreatePooled(null!));
        stream.Position.ShouldBe(0);
    }
}
