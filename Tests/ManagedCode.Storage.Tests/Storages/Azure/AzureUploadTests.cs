using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Core.Primitives;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public class AzureUploadTests : UploadTests<AzuriteContainer>
{
    private const string ObservedBlobName = "metadata.txt";
    private readonly BlobRequestPolicy _requestPolicy = new();

    protected override AzuriteContainer Build()
    {
        return new AzuriteBuilder(ContainerImages.Azurite)
            .WithCommand("--skipApiVersionCheck")
            .Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        var clientOptions = new BlobClientOptions();
        clientOptions.AddPolicy(_requestPolicy, HttpPipelinePosition.PerCall);

        var services = new ServiceCollection();
        services.AddAzureStorageAsDefault(options =>
        {
            options.Container = "managed-code-bucket";
            options.ConnectionString = Container.GetConnectionString();
            options.OriginalOptions = clientOptions;
        });
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UploadAsync_ReturnsCompleteMetadata_WithoutReadingBlobProperties(bool seekable)
    {
        var createResult = await Storage.CreateContainerAsync(TestContext.Current.CancellationToken);
        createResult.IsSuccess.ShouldBeTrue();
        _requestPolicy.Reset();

        var content = Encoding.UTF8.GetBytes("upload response metadata");
        await using Stream source = seekable
            ? new MemoryStream(content)
            : new NonSeekableReadStream(new MemoryStream(content));
        var options = new UploadOptions
        {
            FileName = ObservedBlobName,
            Directory = "uploads",
            MimeType = "text/plain",
            Metadata = new Dictionary<string, string> { ["purpose"] = "regression" }
        };

        var result = await Storage.UploadAsync(source, options, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.FullName.ShouldBe("uploads/metadata.txt");
        result.Value.Name.ShouldBe("metadata.txt");
        result.Value.Container.ShouldBe("managed-code-bucket");
        result.Value.Length.ShouldBe((ulong)content.Length);
        result.Value.MimeType.ShouldBe("text/plain");
        result.Value.Metadata.ShouldNotBeNull();
        result.Value.Metadata!["purpose"].ShouldBe("regression");
        result.Value.CreatedOn.ShouldBe(result.Value.LastModified);
        _requestPolicy.BlobHeadRequestCount.ShouldBe(0);
        _requestPolicy.BlobWriteRequestCount.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(true, 7, "text/plain", "identity", "публікація")]
    [InlineData(true, 0, null, null, "native")]
    [InlineData(true, 4 * 1024 * 1024 + 7, "application/octet-stream", null, "native")]
    [InlineData(false, 4 * 1024 * 1024 + 7, null, null, "native")]
    [InlineData(true, 7, "text/plain", null, " padded ")]
    [InlineData(true, 7, "text/plain", null, "")]
    public async Task WriteObjectAsync_ReturnsCommittedProperties_WithoutReadingBlobProperties(
        bool seekable, int length, string? contentType, string? contentEncoding, string purpose)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var objects = Storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(cancellationToken: cancellationToken);
        var bytes = new byte[length];
        var prefixLength = length == 0 ? 0 : 1;
        var prefixed = new byte[prefixLength + length];
        if (prefixLength > 0)
            prefixed[0] = 1;
        await using var stream = new MemoryStream(prefixed) { Position = prefixLength };
        await using var source = seekable ? null : new NonSeekableReadStream(stream);
        var options = new StorageWriteOptions
        {
            IfAbsent = true,
            ContentType = contentType,
            ContentEncoding = contentEncoding,
            Metadata = new Dictionary<string, string> { ["purpose"] = purpose }
        };
        _requestPolicy.Reset();

        var written = await objects.WriteObjectAsync(ObservedBlobName, (Stream?)source ?? stream, options, cancellationToken);

        _requestPolicy.BlobHeadRequestCount.ShouldBe(0);
        _requestPolicy.BlobWriteRequestCount.ShouldBeGreaterThan(0);
        var persisted = await objects.GetObjectInfoAsync(ObservedBlobName, cancellationToken);
        written.Length.ShouldBe(length);
        (written with { Metadata = persisted.Metadata }).ShouldBe(persisted);
        written.Metadata.Count.ShouldBe(persisted.Metadata.Count);
        written.Metadata["purpose"].ShouldBe(purpose);
        persisted.Metadata["purpose"].ShouldBe(purpose);
        await using var download = await objects.OpenObjectReadAsync(ObservedBlobName, cancellationToken: cancellationToken);
        using var downloaded = new MemoryStream();
        await download.CopyToAsync(downloaded, cancellationToken);
        downloaded.ToArray().ShouldBe(bytes);
    }

    [Theory]
    [InlineData(false, 1, "payload")]
    [InlineData(true, 0, "ayl")]
    public async Task OpenObjectReadAsync_UsesOnlyRequiredHttpRequests(bool range, int expectedHeadCount, string expectedContent)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var objects = Storage.RequireObjectStorage();
        await objects.CreatePrivateContainerAsync(cancellationToken: cancellationToken);
        using var payload = new MemoryStream("payload"u8.ToArray());
        await objects.WriteObjectAsync(ObservedBlobName, payload, cancellationToken: cancellationToken);
        _requestPolicy.Reset();

        var options = range ? new StorageReadOptions { Offset = 1, Length = 3 } : null;
        await using var download = await objects.OpenObjectReadAsync(ObservedBlobName, options, cancellationToken);
        using var reader = new StreamReader(download);
        (await reader.ReadToEndAsync(cancellationToken)).ShouldBe(expectedContent);
        _requestPolicy.BlobHeadRequestCount.ShouldBe(expectedHeadCount);
        _requestPolicy.BlobGetRequestCount.ShouldBe(1);
    }

    private sealed class BlobRequestPolicy : HttpPipelineSynchronousPolicy
    {
        private int _blobHeadRequestCount;
        private int _blobGetRequestCount;
        private int _blobWriteRequestCount;

        public int BlobHeadRequestCount => Volatile.Read(ref _blobHeadRequestCount);
        public int BlobGetRequestCount => Volatile.Read(ref _blobGetRequestCount);
        public int BlobWriteRequestCount => Volatile.Read(ref _blobWriteRequestCount);

        public void Reset()
        {
            Volatile.Write(ref _blobHeadRequestCount, 0);
            Volatile.Write(ref _blobGetRequestCount, 0);
            Volatile.Write(ref _blobWriteRequestCount, 0);
        }

        public override void OnSendingRequest(HttpMessage message)
        {
            if (!message.Request.Uri.Path.EndsWith($"/{ObservedBlobName}", StringComparison.Ordinal))
                return;

            if (message.Request.Method == RequestMethod.Head)
                Interlocked.Increment(ref _blobHeadRequestCount);
            else if (message.Request.Method == RequestMethod.Get)
                Interlocked.Increment(ref _blobGetRequestCount);
            else if (message.Request.Method == RequestMethod.Put)
                Interlocked.Increment(ref _blobWriteRequestCount);
        }
    }

    private sealed class NonSeekableReadStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => inner.Read(buffer);
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                inner.Dispose();

            base.Dispose(disposing);
        }
    }
}
