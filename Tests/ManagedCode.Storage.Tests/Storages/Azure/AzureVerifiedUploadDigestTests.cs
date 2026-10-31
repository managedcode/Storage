using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core.Primitives;
using ManagedCode.Storage.Tests.Common;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public sealed class AzureVerifiedUploadDigestTests : IAsyncLifetime
{
    private const int MultipartPayloadBytes = 4 * 1024 * 1024 + 9;

    private readonly AzuriteContainer _container = new AzuriteBuilder(ContainerImages.Azurite)
        .WithCommand("--skipApiVersionCheck").Build();

    public ValueTask InitializeAsync() => new(_container.StartAsync());
    public ValueTask DisposeAsync() => _container.DisposeAsync();

    [Fact]
    public async Task StreamUpload_WithMatchingDigest_CommitsAndReusesRetry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var storage = CreateStorage();
        var objects = await CreateContainerAsync(storage);
        var bytes = RandomNumberGenerator.GetBytes(MultipartPayloadBytes);
        var digest = Convert.ToHexString(SHA256.HashData(bytes));

        using var first = new MemoryStream(bytes, writable: false);
        var written = await objects.WriteIfAbsentOrSameAsync("archive.zip", first, bytes.LongLength, digest,
            cancellationToken: cancellationToken);
        using var retry = new MemoryStream(bytes, writable: false);
        var replay = await objects.WriteIfAbsentOrSameAsync("archive.zip", retry, bytes.LongLength,
            digest.ToLowerInvariant(), cancellationToken: cancellationToken);

        written.ReusedExisting.ShouldBeFalse();
        written.Sha256.ShouldBe(digest.ToLowerInvariant());
        written.Info.Length.ShouldBe(bytes.LongLength);
        replay.ReusedExisting.ShouldBeTrue();
        replay.Info.ETag.ShouldBe(written.Info.ETag);
    }

    [Fact]
    public async Task StreamUpload_WithDifferentDigest_CommitsNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var storage = CreateStorage();
        var objects = await CreateContainerAsync(storage);
        var bytes = Encoding.UTF8.GetBytes("declared content");
        var otherDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("other content")));

        using var source = new MemoryStream(bytes, writable: false);
        await Should.ThrowAsync<StorageUploadDigestException>(() => objects.WriteIfAbsentOrSameAsync(
            "mismatch.bin", source, bytes.LongLength, otherDigest, cancellationToken: cancellationToken));

        (await objects.ObjectExistsAsync("mismatch.bin", cancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task ByteUpload_WithExpectedDigest_WritesOnlyMatchingContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var storage = CreateStorage();
        var objects = await CreateContainerAsync(storage);
        var bytes = Encoding.UTF8.GetBytes("{\"value\":1}");
        var digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var options = new StorageWriteOptions { ContentType = "application/json" };

        await Should.ThrowAsync<StorageUploadDigestException>(() => objects.WriteBytesIfAbsentOrSameAsync(
            "payload.json", Encoding.UTF8.GetBytes("{\"value\":2}"), digest, options, cancellationToken));
        (await objects.ObjectExistsAsync("payload.json", cancellationToken)).ShouldBeFalse();

        var written = await objects.WriteBytesIfAbsentOrSameAsync("payload.json", bytes, digest, options,
            cancellationToken);
        var replay = await objects.WriteBytesIfAbsentOrSameAsync("payload.json", bytes, digest, options,
            cancellationToken);

        written.ReusedExisting.ShouldBeFalse();
        written.Sha256.ShouldBe(digest);
        replay.ReusedExisting.ShouldBeTrue();
        replay.Info.ETag.ShouldBe(written.Info.ETag);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("zz00000000000000000000000000000000000000000000000000000000000000")]
    public async Task Upload_WithMalformedDigest_RejectsBeforeWriting(string expectedSha256)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var storage = CreateStorage();
        var objects = await CreateContainerAsync(storage);
        var bytes = Encoding.UTF8.GetBytes("content");

        using var source = new MemoryStream(bytes, writable: false);
        (await Should.ThrowAsync<ArgumentException>(() => objects.WriteIfAbsentOrSameAsync(
            "malformed.bin", source, bytes.LongLength, expectedSha256, cancellationToken: cancellationToken)))
            .ParamName.ShouldBe("expectedSha256");
        (await Should.ThrowAsync<ArgumentException>(() => objects.WriteBytesIfAbsentOrSameAsync(
            "malformed.bin", bytes, expectedSha256, cancellationToken: cancellationToken)))
            .ParamName.ShouldBe("expectedSha256");

        (await objects.ObjectExistsAsync("malformed.bin", cancellationToken)).ShouldBeFalse();
    }

    private AzureStorage CreateStorage() => new(new AzureStorageOptions
    {
        ConnectionString = _container.GetConnectionString(),
        Container = $"verified-digest-{Guid.NewGuid():N}",
        CreateContainerIfNotExists = false
    });

    private static async Task<IObjectStorage> CreateContainerAsync(AzureStorage storage)
    {
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(cancellationToken: TestContext.Current.CancellationToken);
        return objects;
    }
}
