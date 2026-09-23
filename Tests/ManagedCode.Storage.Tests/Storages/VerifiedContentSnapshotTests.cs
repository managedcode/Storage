using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Primitives;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages;

public sealed class VerifiedContentSnapshotTests
{
    [Fact]
    public async Task CreateAsync_ReturnsSeekableSnapshotOfExactContent()
    {
        var bytes = Encoding.UTF8.GetBytes("verified content");
        await using var source = new MemoryStream(bytes);
        var digest = Convert.ToHexStringLower(SHA256.HashData(bytes));

        await using var snapshot = await VerifiedContentSnapshot.CreateAsync(source, bytes.Length, digest);

        snapshot.ShouldNotBeNull();
        snapshot!.CanSeek.ShouldBeTrue();
        snapshot.Position.ShouldBe(0);
        using var result = new MemoryStream();
        await snapshot.CopyToAsync(result);
        result.ToArray().ShouldBe(bytes);
    }

    [Theory]
    [InlineData(3, "a")]
    [InlineData(1, "a")]
    [InlineData(2, "b")]
    public async Task CreateAsync_RejectsWrongLengthOrDigest(long expectedLength, string digestSource)
    {
        await using var source = new MemoryStream(Encoding.UTF8.GetBytes("ab"));
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(digestSource)));

        var snapshot = await VerifiedContentSnapshot.CreateAsync(source, expectedLength, digest);

        snapshot.ShouldBeNull();
    }
}
