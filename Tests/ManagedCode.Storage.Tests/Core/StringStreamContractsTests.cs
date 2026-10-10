using System;
using System.IO;
using System.Text;
using ManagedCode.Storage.Core;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Core;

public sealed class StringStreamContractsTests
{
    [Fact]
    public void SeekAndRead_MatchUtf16BytesAndEndOfStream()
    {
        const string text = "Привіт 🌍";
        var bytes = Encoding.Unicode.GetBytes(text);
        using var stream = new StringStream(text);
        stream.ToString().ShouldBe(text);
        stream.Length.ShouldBe(bytes.Length);
        stream.Seek(3, SeekOrigin.Begin).ShouldBe(3);
        stream.ReadByte().ShouldBe(bytes[3]);
        stream.Seek(-2, SeekOrigin.Current).ShouldBe(2);
        var buffer = new byte[5];
        stream.Read(buffer, 1, 3).ShouldBe(3);
        buffer[1..4].ShouldBe(bytes[2..5]);
        stream.Seek(-1, SeekOrigin.End).ShouldBe(bytes.Length - 1);
        stream.ReadByte().ShouldBe(bytes[^1]);
        stream.ReadByte().ShouldBe(-1);
        stream.Seek(5, SeekOrigin.End);
        stream.Read(buffer, 0, buffer.Length).ShouldBe(0);
        stream.ReadByte().ShouldBe(-1);
        Should.Throw<NotSupportedException>(() => stream.SetLength(1));
        Should.Throw<NotSupportedException>(() => stream.Write(buffer, 0, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => stream.Seek(0, (SeekOrigin)42));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    [InlineData(3, 2)]
    public void InvalidReadBounds_AreRejectedWithoutMovingPosition(int offset, int count)
    {
        using var stream = new StringStream("content");
        Should.Throw<ArgumentException>(() => stream.Read(new byte[4], offset, count));
        stream.Position.ShouldBe(0);
    }
}
