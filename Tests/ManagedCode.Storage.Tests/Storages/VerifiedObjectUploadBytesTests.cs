using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Primitives;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages;

public sealed class VerifiedObjectUploadBytesTests
{
    [Fact]
    public async Task LostWriteResponse_ReconcilesOnlyExactStoredBytesAndMetadata()
    {
        var storage = new LostResponseStorage();
        var options = new StorageWriteOptions
        {
            ContentType = "application/json",
            Metadata = new Dictionary<string, string> { ["owner"] = "one" }
        };
        var bytes = Encoding.UTF8.GetBytes("{\"value\":1}");

        var result = await storage.WriteBytesIfAbsentOrSameAsync("payload.json", bytes, options);

        result.ReusedExisting.ShouldBeTrue();
        result.Info.Length.ShouldBe(bytes.Length);
        (await Should.ThrowAsync<StorageOperationException>(() =>
            storage.WriteBytesIfAbsentOrSameAsync("payload.json", bytes,
                options with { Metadata = new Dictionary<string, string> { ["owner"] = "two" } })))
            .IsConflict.ShouldBeTrue();
        (await Should.ThrowAsync<StorageOperationException>(() =>
            storage.WriteBytesIfAbsentOrSameAsync("payload.json", Encoding.UTF8.GetBytes("different"),
                options))).IsConflict.ShouldBeTrue();
    }

    private sealed class LostResponseStorage : IObjectStorage
    {
        private byte[]? _bytes;
        private StorageObjectInfo? _info;

        public Uri ContainerUri { get; } = new("https://example.test/container");

        public Task<StorageObjectInfo> GetObjectInfoAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(_info ?? throw new StorageOperationException("Missing.", 404, new IOException()));

        public Task<Stream> OpenObjectReadAsync(string path, StorageReadOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(_bytes ?? throw new IOException("Missing."), writable: false));

        public async Task<StorageObjectInfo> WriteObjectAsync(string path, Stream content,
            StorageWriteOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (_info is not null)
            {
                throw new StorageOperationException("Conflict.", 412, new IOException());
            }

            using var target = new MemoryStream();
            await content.CopyToAsync(target, cancellationToken);
            _bytes = target.ToArray();
            _info = new StorageObjectInfo(path, "etag-1", _bytes.Length, options?.ContentType,
                options?.ContentEncoding, options?.Metadata ?? new Dictionary<string, string>());
            throw new IOException("The write committed but its response was lost.");
        }

        public Task<StorageContainerInfo> GetContainerInfoAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task CreatePrivateContainerAsync(IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SetContainerMetadataAsync(IReadOnlyDictionary<string, string> metadata,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteContainerIfExistsAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> ObjectExistsAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task SetObjectMetadataAsync(string path, IReadOnlyDictionary<string, string> metadata,
            string? ifMatch = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> DeleteObjectIfExistsAsync(string path, bool includeSnapshots = false,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StorageObjectPage> ListObjectsAsync(string? prefix = null, string? continuationToken = null,
            int pageSize = 100, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
