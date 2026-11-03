using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

namespace ManagedCode.Storage.Aws;

public class BlobStream : Stream
{
    /* Note the that maximum size (as of now) of a file in S3 is 5TB so it isn't
     * safe to assume all uploads will work here.  MAX_PART_SIZE times MAX_PART_COUNT
     * is ~50TB, which is too big for S3. */
    private const long MIN_PART_LENGTH = 5L * 1024 * 1024; // all parts but the last this size or greater
    private const long MAX_PART_LENGTH = 5L * 1024 * 1024 * 1024; // 5GB max per PUT
    private const long MAX_PART_COUNT = 10000; // no more than 10,000 parts total
    private const long DEFAULT_PART_LENGTH = MIN_PART_LENGTH;
    private const int MaxPendingParts = 2;

    private readonly Metadata _metadata = new();
    private readonly IAmazonS3 _s3;
    private bool _disposed;

    public BlobStream(IAmazonS3 s3, string s3uri, long partLength = DEFAULT_PART_LENGTH) : this(s3, new Uri(s3uri), partLength)
    {
    }

    public BlobStream(IAmazonS3 s3, Uri s3uri, long partLength = DEFAULT_PART_LENGTH) : this(s3, s3uri.Host, s3uri.LocalPath.Substring(1), partLength)
    {
    }

    public BlobStream(IAmazonS3 s3, string bucket, string key, long partLength = DEFAULT_PART_LENGTH)
    {
        _s3 = s3;
        _metadata.BucketName = bucket;
        _metadata.Key = key;
        _metadata.PartLength = partLength;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !_disposed;
    public override long Length => _metadata.Length = Math.Max(_metadata.Length, _metadata.Position);

    public override long Position
    {
        get => _metadata.Position;
        set => throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
        {
            base.Dispose(disposing);
            return;
        }

        _disposed = true;
        try
        {
            if (_metadata.Position == 0 && _metadata.UploadId is null)
            {
                _s3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = _metadata.BucketName,
                    Key = _metadata.Key,
                    InputStream = Stream.Null
                }).GetAwaiter().GetResult();
            }
            else
            {
                FlushPart(true);
                CompleteUpload();
            }
        }

        catch (Exception failure)
        {
            AbortUpload(failure);
            throw;
        }
        finally
        {
            _metadata.CurrentStream?.Dispose();
            base.Dispose(disposing);
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        _metadata.Length = Math.Max(_metadata.Length, value);
        _metadata.PartLength = Math.Max(MIN_PART_LENGTH, Math.Min(MAX_PART_LENGTH, _metadata.Length / MAX_PART_COUNT));
    }

    private void StartNewPart()
    {
        if (_metadata.CurrentStream != null)
            FlushPart(false);

        _metadata.CurrentStream = CreatePartStream();
        _metadata.PartLength = Math.Min(MAX_PART_LENGTH, Math.Max(_metadata.PartLength, (_metadata.PartCount / 2 + 1) * MIN_PART_LENGTH));
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        try
        {
            FlushPart(false);
        }
        catch (Exception failure)
        {
            AbortUpload(failure);
            throw;
        }
    }

    private void FlushPart(bool disposing)
    {
        var part = _metadata.CurrentStream;
        if (part is null || (part.Length < MIN_PART_LENGTH && !disposing))
            return;

        if (_metadata.Tasks.Count == MaxPendingParts)
            _metadata.Tasks.Dequeue().GetAwaiter().GetResult();

        if (_metadata.UploadId == null)
            _metadata.UploadId = _s3.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
            {
                BucketName = _metadata.BucketName,
                Key = _metadata.Key
            })
                .GetAwaiter()
                .GetResult()
                .UploadId;

        var index = ++_metadata.PartCount;
        part.Position = 0;
        var request = new UploadPartRequest
        {
            BucketName = _metadata.BucketName,
            Key = _metadata.Key,
            UploadId = _metadata.UploadId,
            PartNumber = index,
            IsLastPart = disposing,
            InputStream = part
        };
        _metadata.CurrentStream = null;
        _metadata.Tasks.Enqueue(UploadPartAsync(request, part, index));
    }

    private Task UploadPartAsync(UploadPartRequest request, Stream part, int index) => Task.Run(async () =>
    {
        await using var input = part.ConfigureAwait(false);
        var response = await _s3.UploadPartAsync(request).ConfigureAwait(false);
        _metadata.PartETags[index] = response.ETag
            ?? throw new InvalidOperationException("S3 multipart upload part did not return an ETag.");
    });

    private static FileStream CreatePartStream()
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose
        };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        return new FileStream(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), options);
    }

    private void AbortUpload(Exception failure)
    {
        _disposed = true;
        try
        {
            Task.WhenAll(_metadata.Tasks).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
            if (_metadata.UploadId is { } uploadId)
            {
                _s3.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
                {
                    BucketName = _metadata.BucketName,
                    Key = _metadata.Key,
                    UploadId = uploadId
                }).GetAwaiter().GetResult();
            }
        }
        catch (Exception cleanupFailure)
        {
            throw new AggregateException(failure, cleanupFailure);
        }
        finally
        {
            _metadata.CurrentStream?.Dispose();
            _metadata.CurrentStream = null;
        }
    }

    private void CompleteUpload()
    {
        Task.WhenAll(_metadata.Tasks).GetAwaiter().GetResult();

        if (Length > 0)
            _s3.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
            {
                BucketName = _metadata.BucketName,
                Key = _metadata.Key,
                PartETags = _metadata.PartETags
                        .OrderBy(e => e.Key)
                        .Select(e => new PartETag(e.Key, e.Value))
                        .ToList(),
                UploadId = _metadata.UploadId ?? throw new InvalidOperationException("UploadId must be initialized before completing the upload.")
            })
                .GetAwaiter()
                .GetResult();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length);
        if (count > buffer.Length - offset)
            throw new ArgumentException("The requested range exceeds the buffer length.", nameof(count));
        if (count == 0) return;

        try
        {
            WriteParts(buffer, offset, count);
        }
        catch (Exception failure)
        {
            AbortUpload(failure);
            throw;
        }
    }

    private void WriteParts(byte[] buffer, int offset, int count)
    {
        // write as much of the buffer as will fit to the current part, and if needed
        // allocate a new part and continue writing to it (and so on).
        while (count > 0)
        {
            if (_metadata.CurrentStream == null || _metadata.CurrentStream.Length >= _metadata.PartLength)
                StartNewPart();

            var currentStream = _metadata.CurrentStream ?? throw new InvalidOperationException("Multipart upload stream was not initialized.");
            var remaining = _metadata.PartLength - currentStream.Length;
            var written = (int)Math.Min(count, remaining);
            currentStream.Write(buffer, offset, written);

            _metadata.Position += written;
            count -= written;
            offset += written;
        }
    }

    internal class Metadata
    {
        public string BucketName = string.Empty;
        public Stream? CurrentStream;
        public string Key = string.Empty;
        public long Length; // based on bytes written or SetLength, whichever is larger (no truncation)

        public int PartCount;
        public ConcurrentDictionary<int, string> PartETags = new();
        public long PartLength = DEFAULT_PART_LENGTH;

        public long Position; // based on bytes written

        public readonly Queue<Task> Tasks = new();
        public string? UploadId;
    }
}
