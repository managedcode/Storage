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
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.Azure;

public class AzureUploadTests : UploadTests<AzuriteContainer>
{
    private readonly UploadRequestPolicy _requestPolicy = new();

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
        var createResult = await Storage.CreateContainerAsync();
        createResult.IsSuccess.ShouldBeTrue();
        _requestPolicy.Reset();

        var content = Encoding.UTF8.GetBytes("upload response metadata");
        await using Stream source = seekable
            ? new MemoryStream(content)
            : new NonSeekableReadStream(new MemoryStream(content));
        var options = new UploadOptions
        {
            FileName = "metadata.txt",
            Directory = "uploads",
            MimeType = "text/plain",
            Metadata = new Dictionary<string, string> { ["purpose"] = "regression" }
        };

        var result = await Storage.UploadAsync(source, options);

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

    private sealed class UploadRequestPolicy : HttpPipelineSynchronousPolicy
    {
        private int _blobHeadRequestCount;
        private int _blobWriteRequestCount;

        public int BlobHeadRequestCount => Volatile.Read(ref _blobHeadRequestCount);
        public int BlobWriteRequestCount => Volatile.Read(ref _blobWriteRequestCount);

        public void Reset()
        {
            Volatile.Write(ref _blobHeadRequestCount, 0);
            Volatile.Write(ref _blobWriteRequestCount, 0);
        }

        public override void OnSendingRequest(HttpMessage message)
        {
            if (!message.Request.Uri.Path.EndsWith("/metadata.txt", StringComparison.Ordinal))
                return;

            if (message.Request.Method == RequestMethod.Head)
                Interlocked.Increment(ref _blobHeadRequestCount);
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
