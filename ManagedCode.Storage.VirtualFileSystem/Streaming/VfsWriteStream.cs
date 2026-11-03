using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Metadata;
using ManagedCode.Storage.VirtualFileSystem.Options;

namespace ManagedCode.Storage.VirtualFileSystem.Streaming;

internal class VfsWriteStream : Stream
{
    private readonly IVirtualFileSystem _vfs;
    private readonly VfsPath _path;
    private readonly string _blobKey;
    private readonly WriteOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly LocalFile _bufferFile;
    private readonly FileStream _bufferStream;
    private readonly CancellationToken _cancellationToken;
    private bool _abortUpload;
    private bool _disposed;

    public VfsWriteStream(
        IVirtualFileSystem vfs,
        VfsPath path,
        WriteOptions options,
        IMemoryCache cache,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        _vfs = vfs ?? throw new ArgumentNullException(nameof(vfs));
        _path = path;
        _blobKey = path.ToBlobKey();
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cancellationToken = cancellationToken;

        _bufferFile = LocalFile.FromRandomNameWithExtension(_blobKey);
        _bufferStream = new FileStream(_bufferFile.FilePath, new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        });
    }

    public override bool CanRead => false;
    public override bool CanSeek => _bufferStream.CanSeek;
    public override bool CanWrite => !_disposed && _bufferStream.CanWrite;
    public override long Length => _bufferStream.Length;

    public override long Position { get => _bufferStream.Position; set => _bufferStream.Position = value; }
    public override void Flush() => _bufferStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _bufferStream.FlushAsync(cancellationToken);
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Read operations are not supported on write streams");
    public override long Seek(long offset, SeekOrigin origin) => _bufferStream.Seek(offset, origin);
    public override void SetLength(long value) => _bufferStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        try
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _bufferStream.Write(buffer);
        }
        catch
        {
            _abortUpload = true;
            throw;
        }
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            _cancellationToken.ThrowIfCancellationRequested();
            await _bufferStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _abortUpload = true;
            throw;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposed = true;
            using var file = _bufferFile;
            using var buffer = _bufferStream;
            // Provider continuations must be able to run while the synchronous caller waits.
            var upload = SynchronizationContext.Current is null && TaskScheduler.Current == TaskScheduler.Default
                ? UploadBufferedDataAsync()
                : Task.Run(UploadBufferedDataAsync);
            upload.GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            if (_disposed)
                return;

            _disposed = true;
            await using var file = _bufferFile.ConfigureAwait(false);
            await using var buffer = _bufferStream.ConfigureAwait(false);
            await UploadBufferedDataAsync().ConfigureAwait(false);
        }
        finally
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task UploadBufferedDataAsync()
    {
        if (_abortUpload)
            return;

        _cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Uploading buffered data: {BlobKey}, size: {Size}", _blobKey, _bufferStream.Length);

        try
        {
            await _bufferStream.FlushAsync(_cancellationToken).ConfigureAwait(false);
            _bufferStream.Position = 0;

            var uploadOptions = new UploadOptions(_blobKey)
            {
                MimeType = _options.ContentType,
                Metadata = _options.Metadata
            };

            _cancellationToken.ThrowIfCancellationRequested();
            var result = await _vfs.Storage.UploadAsync(_bufferStream, uploadOptions)
                .ConfigureAwait(false);
            if (result.IsFailed)
                throw new VfsOperationException($"Failed to upload data for: {_blobKey}. Error: {result.Problem}");

            VfsCacheKey.InvalidateFile(_cache, _vfs, _path);
            _logger.LogDebug("Successfully uploaded data: {BlobKey}", _blobKey);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error uploading buffered data: {BlobKey}", _blobKey);
            if (ex is VfsOperationException)
                throw;

            throw new VfsOperationException($"Failed to upload data for: {_blobKey}", ex);
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
