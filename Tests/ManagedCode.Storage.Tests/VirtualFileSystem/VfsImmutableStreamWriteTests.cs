using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Core.Primitives;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public sealed class VfsImmutableStreamWriteTests : IAsyncLifetime
{
    private const int MultipartPayloadBytes = 4 * 1024 * 1024 + 17;

    private readonly AzuriteContainer _container = new AzuriteBuilder(ContainerImages.Azurite)
        .WithCommand("--skipApiVersionCheck").Build();

    public ValueTask InitializeAsync() => new(_container.StartAsync());
    public ValueTask DisposeAsync() => _container.DisposeAsync();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StreamWrite_RetryWithSameContent_ReusesExistingObject(bool cache)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var provider = CreateProvider(cache);
        await using var scope = provider.CreateAsyncScope();
        var vfs = await CreateVfsAsync(scope, cancellationToken);
        var path = new VfsPath("/skills/imports/payload.zip");
        var previous = await vfs.GetFileAsync(path, cancellationToken);
        (await previous.GetMetadataAsync(cancellationToken)).ShouldBeEmpty();
        (await vfs.FileExistsAsync(path, cancellationToken)).ShouldBeFalse();
        var bytes = RandomNumberGenerator.GetBytes(MultipartPayloadBytes);
        var metadata = new Dictionary<string, string> { ["ownerUserId"] = "exact-owner", ["fileName"] = "навичка.zip" };
        var options = new StorageWriteOptions { ContentType = "application/zip", Metadata = metadata };

        using var firstSource = new NonSeekableReadStream(bytes);
        var written = await vfs.WriteIfAbsentOrSameAsync(path, firstSource, bytes.LongLength, options, cancellationToken);

        written.ReusedExisting.ShouldBeFalse();
        written.Sha256.ShouldBe(Convert.ToHexStringLower(SHA256.HashData(bytes)));
        written.Info.Length.ShouldBe(bytes.LongLength);
        (await vfs.FileExistsAsync(path, cancellationToken)).ShouldBeTrue();
        AssertMetadata(await previous.GetMetadataAsync(cancellationToken), metadata);
        var fresh = await vfs.GetFileAsync(path, cancellationToken);
        (await fresh.ReadAllBytesAsync(cancellationToken)).ShouldBe(bytes);

        using var retrySource = new NonSeekableReadStream(bytes);
        var replay = await vfs.WriteIfAbsentOrSameAsync(path, retrySource, bytes.LongLength, options, cancellationToken);

        replay.ReusedExisting.ShouldBeTrue();
        replay.Info.ETag.ShouldBe(written.Info.ETag);
        replay.Sha256.ShouldBe(written.Sha256);
        (await fresh.ReadAllBytesAsync(cancellationToken)).ShouldBe(bytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StreamWrite_DifferentContent_KeepsConflictAndOriginalBytes(bool cache)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var provider = CreateProvider(cache);
        await using var scope = provider.CreateAsyncScope();
        var vfs = await CreateVfsAsync(scope, cancellationToken);
        var path = new VfsPath("/skills/imports/conflict.zip");
        var original = Encoding.UTF8.GetBytes("original");
        var altered = Encoding.UTF8.GetBytes("altered!");
        var options = new StorageWriteOptions { ContentType = "application/zip" };
        using (var source = new NonSeekableReadStream(original))
        {
            (await vfs.WriteIfAbsentOrSameAsync(path, source, original.LongLength, options, cancellationToken))
                .ReusedExisting.ShouldBeFalse();
        }

        using var conflicting = new NonSeekableReadStream(altered);
        var conflict = await Should.ThrowAsync<StorageOperationException>(() =>
            vfs.WriteIfAbsentOrSameAsync(path, conflicting, altered.LongLength, options, cancellationToken));

        conflict.IsConflict.ShouldBeTrue();
        (await vfs.FileExistsAsync(path, cancellationToken)).ShouldBeTrue();
        var file = await vfs.GetFileAsync(path, cancellationToken);
        (await file.ReadAllBytesAsync(cancellationToken)).ShouldBe(original);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StreamWrite_LengthMismatch_FailsWithoutCommitting(bool cache)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var provider = CreateProvider(cache);
        await using var scope = provider.CreateAsyncScope();
        var vfs = await CreateVfsAsync(scope, cancellationToken);
        var tooLongPath = new VfsPath("/skills/imports/too-long.zip");
        var tooShortPath = new VfsPath("/skills/imports/too-short.zip");
        (await vfs.FileExistsAsync(tooLongPath, cancellationToken)).ShouldBeFalse();
        (await vfs.FileExistsAsync(tooShortPath, cancellationToken)).ShouldBeFalse();

        using var tooLong = new NonSeekableReadStream(Encoding.UTF8.GetBytes("too long"));
        (await Should.ThrowAsync<StorageUploadLengthException>(() =>
            vfs.WriteIfAbsentOrSameAsync(tooLongPath, tooLong, 3, cancellationToken: cancellationToken)))
            .TooLarge.ShouldBeTrue();
        using var tooShort = new NonSeekableReadStream(Encoding.UTF8.GetBytes("short"));
        (await Should.ThrowAsync<StorageUploadLengthException>(() =>
            vfs.WriteIfAbsentOrSameAsync(tooShortPath, tooShort, 9, cancellationToken: cancellationToken)))
            .TooLarge.ShouldBeFalse();

        (await vfs.StorageFileExistsAsync(tooLongPath, cancellationToken)).ShouldBeFalse();
        (await vfs.StorageFileExistsAsync(tooShortPath, cancellationToken)).ShouldBeFalse();
        (await vfs.FileExistsAsync(tooLongPath, cancellationToken)).ShouldBeFalse();
        (await vfs.FileExistsAsync(tooShortPath, cancellationToken)).ShouldBeFalse();
    }

    private static async Task<IVirtualFileSystem> CreateVfsAsync(AsyncServiceScope scope, CancellationToken cancellationToken)
    {
        var vfs = scope.ServiceProvider.GetRequiredService<IVirtualFileSystem>();
        await vfs.Storage.RequireObjectStorage().CreatePrivateContainerAsync(cancellationToken: cancellationToken);
        return vfs;
    }

    private ServiceProvider CreateProvider(bool cache)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var containerName = $"vfs-stream-{Guid.NewGuid():N}";
        services.AddAzureStorageAsDefault(options =>
        {
            options.ConnectionString = _container.GetConnectionString();
            options.Container = containerName;
            options.CreateContainerIfNotExists = true;
        });
        services.AddVirtualFileSystem(options =>
        {
            options.DefaultContainer = containerName;
            options.EnableCache = cache;
        });
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static void AssertMetadata(IReadOnlyDictionary<string, string> actual, Dictionary<string, string> expected)
    {
        actual.Count.ShouldBe(expected.Count);
        foreach (var item in expected)
        {
            actual[item.Key].ShouldBe(item.Value);
        }
    }

    private sealed class NonSeekableReadStream(byte[] content) : Stream
    {
        private readonly MemoryStream _inner = new(content, writable: false);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
