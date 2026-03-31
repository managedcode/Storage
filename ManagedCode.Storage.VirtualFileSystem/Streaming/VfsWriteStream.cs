using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Options;

namespace ManagedCode.Storage.VirtualFileSystem.Streaming;

internal class VfsWriteStream : Stream
{
    private readonly IStorage _storage;
    private readonly string _blobKey;
    private readonly WriteOptions _options;
    private readonly IMemoryCache _cache;
    private readonly VfsOptions _vfsOptions;
    private readonly ILogger _logger;
    private readonly LocalFile _bufferFile;
    private readonly FileStream _bufferStream;
    private bool _disposed;

    public VfsWriteStream(
        IStorage storage,
        string blobKey,
        WriteOptions options,
        IMemoryCache cache,
        VfsOptions vfsOptions,
        ILogger logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _blobKey = blobKey ?? throw new ArgumentNullException(nameof(blobKey));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _vfsOptions = vfsOptions ?? throw new ArgumentNullException(nameof(vfsOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _bufferFile = LocalFile.FromRandomNameWithExtension(blobKey);
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
        ThrowIfDisposed();
        _bufferStream.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        _bufferStream.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _bufferStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _bufferStream.WriteAsync(buffer, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                UploadBufferedDataAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading data during stream dispose: {BlobKey}", _blobKey);
            }
            finally
            {
                _bufferStream.Dispose();
                _bufferFile.Dispose();
                _disposed = true;
            }
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed) { await base.DisposeAsync(); return; }

        try
        {
            await UploadBufferedDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading data during stream dispose: {BlobKey}", _blobKey);
            throw;
        }
        finally
        {
            await _bufferStream.DisposeAsync();
            await _bufferFile.DisposeAsync();
            _disposed = true;
        }

        await base.DisposeAsync();
    }

    private async Task UploadBufferedDataAsync()
    {
        if (_bufferStream.Length == 0) { _logger.LogDebug("No data to upload for: {BlobKey}", _blobKey); return; }

        _logger.LogDebug("Uploading buffered data: {BlobKey}, size: {Size}", _blobKey, _bufferStream.Length);

        try
        {
            await _bufferStream.FlushAsync();
            _bufferStream.Position = 0;

            var uploadOptions = new UploadOptions(_blobKey)
            {
                MimeType = _options.ContentType,
                Metadata = _options.Metadata
            };

            var result = await _storage.UploadAsync(_bufferStream, uploadOptions);
            if (!result.IsSuccess)
                throw new VfsOperationException($"Failed to upload data for: {_blobKey}. Error: {result.Problem}");

            InvalidateCache();
            _logger.LogDebug("Successfully uploaded data: {BlobKey}", _blobKey);
        }
        catch (Exception ex) when (ex is not VfsOperationException)
        {
            _logger.LogError(ex, "Error uploading buffered data: {BlobKey}", _blobKey);
            throw new VfsOperationException($"Failed to upload data for: {_blobKey}", ex);
        }
    }

    private void InvalidateCache()
    {
        if (!_vfsOptions.EnableCache)
            return;

        _cache.Remove($"file_exists:{_vfsOptions.DefaultContainer}:{_blobKey}");
        _cache.Remove($"file_metadata:{_vfsOptions.DefaultContainer}:{_blobKey}");
        _cache.Remove($"file_custom_metadata:{_vfsOptions.DefaultContainer}:{_blobKey}");
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
