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

/// <summary>
/// Write stream implementation for VFS that buffers data and uploads on dispose
/// </summary>
internal class VfsWriteStream : Stream
{
    private readonly IStorage _storage;
    private readonly string _blobKey;
    private readonly WriteOptions _options;
    private readonly IMemoryCache _cache;
    private readonly VfsOptions _vfsOptions;
    private readonly ILogger _logger;
    private readonly MemoryStream _buffer;
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
        
        _buffer = new MemoryStream();
    }

    public override bool CanRead => false;
    public override bool CanSeek => _buffer.CanSeek;
    public override bool CanWrite => !_disposed && _buffer.CanWrite;
    public override long Length => _buffer.Length;

    public override long Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    public override void Flush()
    {
        _buffer.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _buffer.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("Read operations are not supported on write streams");
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _buffer.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _buffer.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        _buffer.Write(buffer, offset, count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        await _buffer.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _buffer.WriteAsync(buffer, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                // Upload the buffered data
                UploadBufferedDataAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading data during stream dispose: {BlobKey}", _blobKey);
            }
            finally
            {
                _buffer.Dispose();
                _disposed = true;
            }
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
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
                await _buffer.DisposeAsync();
                _disposed = true;
            }
        }

        await base.DisposeAsync();
    }

    private async Task UploadBufferedDataAsync()
    {
        if (_buffer.Length == 0)
        {
            _logger.LogDebug("No data to upload for: {BlobKey}", _blobKey);
            return;
        }

        _logger.LogDebug("Uploading buffered data: {BlobKey}, size: {Size}", _blobKey, _buffer.Length);

        try
        {
            _buffer.Position = 0;
            
            var uploadOptions = new UploadOptions(_blobKey)
            {
                MimeType = _options.ContentType,
                Metadata = _options.Metadata
            };

            var result = await _storage.UploadAsync(_buffer, uploadOptions);
            
            if (!result.IsSuccess)
            {
                throw new VfsOperationException($"Failed to upload data for: {_blobKey}. Error: {result.Problem}");
            }

            // Invalidate cache after successful upload
            if (_vfsOptions.EnableCache)
            {
                var cacheKey = $"file_exists:{_vfsOptions.DefaultContainer}:{_blobKey}";
                _cache.Remove(cacheKey);
                var metadataCacheKey = $"file_metadata:{_vfsOptions.DefaultContainer}:{_blobKey}";
                _cache.Remove(metadataCacheKey);
            }

            _logger.LogDebug("Successfully uploaded data: {BlobKey}", _blobKey);
        }
        catch (Exception ex) when (!(ex is VfsOperationException))
        {
            _logger.LogError(ex, "Error uploading buffered data: {BlobKey}", _blobKey);
            throw new VfsOperationException($"Failed to upload data for: {_blobKey}", ex);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VfsWriteStream));
    }
}