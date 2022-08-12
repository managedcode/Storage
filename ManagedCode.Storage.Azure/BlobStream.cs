using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace ManagedCode.Storage.Azure;

public class BlobStream : Stream
{
    private const string MetadataLengthKey = "STREAM_LENGTH";
    private const int PageSizeInBytes = 512;
    public const int DefaultBufferSize = 1024 * 1024 * 4;
    private readonly PageBlobClient _pageBlob;

    public BlobStream(string connectionString, string container, string fileName)
        : this(GetClient(connectionString, container, fileName))
    {
    }

    public BlobStream(PageBlobClient pageBlob)
    {
        _pageBlob = pageBlob;
        _pageBlob.CreateIfNotExists(0);
    }

    private long BlobLength => _pageBlob.GetProperties().Value.ContentLength;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length
    {
        get
        {
            var realLength = 0L;
            var metadata = _pageBlob.GetProperties().Value.Metadata;
            if (metadata.TryGetValue(MetadataLengthKey, out var length))
            {
                if (long.TryParse(length, out realLength))
                {
                    return realLength;
                }
            }

            SetLengthInternal(realLength);
            return realLength;
        }
    }

    public override long Position { get; set; }

    private static PageBlobClient GetClient(string connectionString, string container, string fileName)
    {
        BlobServiceClient blobServiceClient = new(connectionString);
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(container);
        blobContainerClient.CreateIfNotExists();
        var pageBlobClient = blobContainerClient.GetPageBlobClient(fileName);
        return pageBlobClient;
    }

    public override void Flush()
    {
        //all is flushed
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            case SeekOrigin.End:
                Position = Length - offset;
                break;
        }

        return Position;
    }

    public override void SetLength(long value)
    {
        if (value > Length)
        {
            var newSize = NextPageAddress(value);
            _pageBlob.Resize(newSize);
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = 0;
        using (var stream = _pageBlob.OpenRead(false, Position))
        {
            bytesRead = stream.Read(buffer, offset, count);
            if (Position + count > Length)
            {
                bytesRead = (int)(Length - Position);
            }
        }

        Position += bytesRead;
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = 0;
        using (var stream = await _pageBlob.OpenReadAsync(false, Position, cancellationToken: cancellationToken))
        {
            bytesRead = await stream.ReadAsync(buffer, offset, count, cancellationToken);
            if (Position + count > Length)
            {
                bytesRead = (int)(Length - Position);
            }
        }

        Position += bytesRead;
        return bytesRead;
    }

    private void EnsureCapacity(long position)
    {
        if (BlobLength < position)
        {
            var newSize = NextPageAddress(position);
            _pageBlob.Resize(newSize);
        }
    }
    
    private async Task EnsureCapacityAsync(long position)
    {
        if (BlobLength < position)
        {
            var newSize = NextPageAddress(position);
            await _pageBlob.ResizeAsync(newSize);
        }
    }
    
    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureCapacity(Position + count);

        var pageStartAddress = PreviousPageAddress(Position);
        var pageBytes = NextPageAddress(Position + count) - pageStartAddress;
        var offsetInFirstPage = (int)(Position % PageSizeInBytes);
        var offsetInLastPage = (offsetInFirstPage + count) % PageSizeInBytes;

        var bufferToMerge = new byte[pageBytes];
        if (offsetInFirstPage > 0 || (pageBytes > PageSizeInBytes && offsetInLastPage > 0))
        {
            var localCount = (int)(pageBytes - PageSizeInBytes);
            using (var stream = _pageBlob.OpenRead(false, pageStartAddress))
            {
                _ = stream.Read(bufferToMerge, 0, localCount);
            }
        }

        Buffer.BlockCopy(buffer, offset, bufferToMerge, offsetInFirstPage, count);
        
        EnsureCapacity(pageStartAddress + bufferToMerge.Length);

        using (var stream = _pageBlob.OpenWrite(false, pageStartAddress))
        {
            stream.Write(bufferToMerge, 0, bufferToMerge.Length);
            stream.Flush();
        }
        
        Position += count;
        if (Position > Length)
        {
            SetLengthInternal(Position);
        }
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        //await EnsureCapacityAsync(Position + count);
        
        var pageStartAddress = PreviousPageAddress(Position);
        var pageBytes = NextPageAddress(Position + count) - pageStartAddress;
        var offsetInFirstPage = (int)(Position % PageSizeInBytes);
        var offsetInLastPage = (offsetInFirstPage + count) % PageSizeInBytes;

        var bufferToMerge = new byte[pageBytes];
        if (offsetInFirstPage > 0 || (pageBytes > PageSizeInBytes && offsetInLastPage > 0))
        {
            var localCount = (int)(pageBytes - PageSizeInBytes);
            using (var stream = await _pageBlob.OpenReadAsync(false, pageStartAddress, cancellationToken: cancellationToken))
            {
                _ = await stream.ReadAsync(bufferToMerge, 0, localCount, cancellationToken);
            }
        }

        Buffer.BlockCopy(buffer, offset, bufferToMerge, offsetInFirstPage, count);
        
        await EnsureCapacityAsync(pageStartAddress + bufferToMerge.Length);
      
        using (var stream = await _pageBlob.OpenWriteAsync(false, pageStartAddress, cancellationToken: cancellationToken))
        {
            await stream.WriteAsync(bufferToMerge, 0, bufferToMerge.Length, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        Position += count;
        if (Position > Length)
        {
            await SetLengthInternalAsync(Position, cancellationToken);
        }
    }

    private long NextPageAddress(long position)
    {
        var previousPageAddress = PreviousPageAddress(position);
        return previousPageAddress + PageSizeInBytes;
    }

    private long PreviousPageAddress(long position)
    {
        var previousPageAddress = position - position % PageSizeInBytes;
        return previousPageAddress;
    }

    public static BlobStream Open(PageBlobClient pageBlob)
    {
        if (!pageBlob.Exists())
        {
            pageBlob.Create(0);
        }

        return new BlobStream(pageBlob);
    }

    private void SetLengthInternal(long newLength)
    {
        _pageBlob.SetMetadata(new Dictionary<string, string>
        {
            [MetadataLengthKey] = newLength.ToString()
        });
    }

    private Task SetLengthInternalAsync(long newLength, CancellationToken cancellationToken = default)
    {
        return _pageBlob.SetMetadataAsync(new Dictionary<string, string>
        {
            [MetadataLengthKey] = newLength.ToString()
        }, cancellationToken: cancellationToken);
    }
}