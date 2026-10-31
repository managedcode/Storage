using System;
using System.Buffers;
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

public sealed class AzureObjectStorageByteTests : IAsyncLifetime
{
    private readonly AzuriteContainer _container = new AzuriteBuilder(ContainerImages.Azurite)
        .WithCommand("--skipApiVersionCheck").Build();

    public ValueTask InitializeAsync() => new(_container.StartAsync());
    public ValueTask DisposeAsync() => _container.DisposeAsync();

    [Fact]
    public async Task WriteObjectBytes_WritesExactSlice_AndKeepsIfAbsentConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(cancellationToken: cancellationToken);
        var buffer = Encoding.UTF8.GetBytes("prefix-original-suffix");
        var options = new StorageWriteOptions { IfAbsent = true, ContentType = "application/octet-stream" };

        var written = await objects.WriteObjectAsync("file.bin", buffer.AsMemory(7, 8), options, cancellationToken);

        written.Length.ShouldBe(8);
        written.ContentType.ShouldBe("application/octet-stream");
        (await ReadTextAsync(objects, "file.bin")).ShouldBe("original");
        (await Should.ThrowAsync<StorageOperationException>(() =>
            objects.WriteObjectAsync("file.bin", buffer.AsMemory(0, 6), options, cancellationToken))).IsConflict.ShouldBeTrue();
        (await ReadTextAsync(objects, "file.bin")).ShouldBe("original");

        using var nonArray = new NonArrayMemory(Encoding.UTF8.GetBytes("copied once"));
        (await objects.WriteObjectAsync("non-array.bin", nonArray.Memory, options, cancellationToken)).Length.ShouldBe(11);
        (await ReadTextAsync(objects, "non-array.bin")).ShouldBe("copied once");
    }

    [Fact]
    public async Task StagePartBytes_CommitsSlicesInRequestedOrder_AndKeepsIfAbsentConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var storage = CreateStorage();
        var multipart = storage.RequireMultipartStorage();
        await multipart.CreatePrivateContainerAsync(cancellationToken: cancellationToken);
        var buffer = Encoding.UTF8.GetBytes("firstsecond");
        var firstId = Convert.ToBase64String(Encoding.UTF8.GetBytes("0001"));
        var secondId = Convert.ToBase64String(Encoding.UTF8.GetBytes("0002"));

        await multipart.StagePartAsync("archive", firstId, buffer.AsMemory(0, 5), cancellationToken);
        await multipart.StagePartAsync("archive", secondId, buffer.AsMemory(5), cancellationToken);
        var committed = await multipart.CommitPartsAsync("archive", [secondId, firstId],
            new StorageWriteOptions { IfAbsent = true }, cancellationToken);

        committed.Length.ShouldBe(11);
        (await ReadTextAsync(multipart, "archive")).ShouldBe("secondfirst");
        await multipart.StagePartAsync("archive", firstId, buffer.AsMemory(0, 5), cancellationToken);
        (await Should.ThrowAsync<StorageOperationException>(() => multipart.CommitPartsAsync("archive", [firstId],
            new StorageWriteOptions { IfAbsent = true }, cancellationToken))).IsConflict.ShouldBeTrue();
        (await ReadTextAsync(multipart, "archive")).ShouldBe("secondfirst");
    }

    [Fact]
    public async Task WriteBytesIfAbsentOrSame_WithArraySlice_ReusesExactRetry_AndRejectsDifferentBytes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var storage = CreateStorage();
        var objects = storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(cancellationToken: cancellationToken);
        var first = Encoding.UTF8.GetBytes("[payload]").AsMemory(1, 7);
        var retry = Encoding.UTF8.GetBytes("--payload--").AsMemory(2, 7);
        var options = new StorageWriteOptions { ContentType = "text/plain" };

        var written = await objects.WriteBytesIfAbsentOrSameAsync("payload.txt", first, options, cancellationToken);
        var replay = await objects.WriteBytesIfAbsentOrSameAsync("payload.txt", retry, options, cancellationToken);

        written.ReusedExisting.ShouldBeFalse();
        written.Info.Length.ShouldBe(7);
        written.Sha256.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("payload"))));
        replay.ReusedExisting.ShouldBeTrue();
        replay.Info.ETag.ShouldBe(written.Info.ETag);
        (await Should.ThrowAsync<StorageOperationException>(() => objects.WriteBytesIfAbsentOrSameAsync(
            "payload.txt", Encoding.UTF8.GetBytes("altered"), options, cancellationToken))).IsConflict.ShouldBeTrue();
        (await ReadTextAsync(objects, "payload.txt")).ShouldBe("payload");
    }

    private AzureStorage CreateStorage() => new(new AzureStorageOptions
    {
        ConnectionString = _container.GetConnectionString(),
        Container = $"object-bytes-{Guid.NewGuid():N}",
        CreateContainerIfNotExists = false
    });

    private static async Task<string> ReadTextAsync(IObjectStorage objects, string path)
    {
        await using var stream = await objects.OpenObjectReadAsync(path, cancellationToken: TestContext.Current.CancellationToken);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Memory whose owner exposes no backing array, so writes take the single-copy path.</summary>
    private sealed class NonArrayMemory(byte[] content) : MemoryManager<byte>
    {
        public override Span<byte> GetSpan() => content;
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();

        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
