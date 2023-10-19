using System;
using System.Collections.Generic;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace ManagedCode.Storage.Azure;

public class BlobStream : Stream
{
    // TODO: add same thing to google and aws
    private const string MetadataLengthKey = "STREAM_LENGTH";
    private const int PageSizeInBytes = 512;
    public const int DefaultBufferSize = 1024 * 1024 * 4;
    private readonly PageBlobClient _pageBlob;
    private readonly object _syncRoot = new();

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
            var contentLenght = _pageBlob.GetProperties().Value.ContentLength;
            if (metadata.TryGetValue(MetadataLengthKey, out var length))
            {
                if (long.TryParse(length, out realLength))
                {
                    return realLength;
                }
            }

            SetLengthInternal(contentLenght);
            return contentLenght;
        }
    }

    public override long Position { get; set; }

    public static BlobStream OpenStream(PageBlobClient pageBlob)
    {
        if (!pageBlob.Exists())
        {
            pageBlob.Create(0);
        }

        return new BlobStream(pageBlob);
    }

    public static BufferedStream OpenBufferedStream(PageBlobClient pageBlob)
    {
        if (!pageBlob.Exists())
        {
            pageBlob.Create(0);
        }

        return new BufferedStream(new BlobStream(pageBlob), DefaultBufferSize);
    }

    public static BufferedStream OpenBufferedStream(string connectionString, string container, string fileName)
    {
        return new BufferedStream(new BlobStream(connectionString, container, fileName), DefaultBufferSize);
    }

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
                Position = Length + offset;
                break;
        }

        return Position;
    }

    public override void SetLength(long value)
    {
        var length = Length;
        if (value != length)
        {
            var newSize = NextPageAddress(value);
            _pageBlob.Resize(newSize);
        }

        SetLengthInternal(value);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        lock (_syncRoot)
        {
            var bytesRead = 0;
            var length = Length;

            if (Position + count > length)
            {
                count = (int)(length - Position);
            }

            using (var stream = _pageBlob.OpenRead(false, Position))
            {
                bytesRead = stream.Read(buffer, offset, count);
            }

            Position += bytesRead;
            return bytesRead;
        }
    }

    private void EnsureCapacity(long position)
    {
        if (BlobLength < position)
        {
            var newSize = NextPageAddress(position);
            _pageBlob.Resize(newSize);
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        lock (_syncRoot)
        {
            var pageStartAddress = PreviousPageAddress(Position);
            var pageBytes = NextPageAddress(Position + count) - pageStartAddress;
            var offsetInFirstPage = (int)(Position % PageSizeInBytes);
            var offsetInLastPage = (offsetInFirstPage + count) % PageSizeInBytes;

            var bufferToMerge = new byte[pageBytes];
            if (offsetInFirstPage > 0 || (pageBytes > PageSizeInBytes && offsetInLastPage > 0))
            {
                //var localCount = (int)(pageBytes - PageSizeInBytes);
                using (var stream = _pageBlob.OpenRead(false, pageStartAddress))
                {
                    _ = stream.Read(bufferToMerge, 0, (int)pageBytes);
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

    private void SetLengthInternal(long newLength)
    {
        _pageBlob.SetMetadata(new Dictionary<string, string>
        {
            [MetadataLengthKey] = newLength.ToString()
        });
    }
}