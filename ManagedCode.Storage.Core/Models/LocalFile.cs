using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Core.Models;

public class LocalFile : IDisposable, IAsyncDisposable
{
    private readonly object _lockObject = new();
    private bool _disposed;
    private FileStream? _stream;

    public LocalFile(string? path = null, bool keepAlive = false)
    {
        path ??= Path.GetTempFileName();

        string? directory;
        KeepAlive = keepAlive;

        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            directory = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);
            if (directory != null)
                FilePath = Path.Combine(directory, $"{name}.tmp");
        }
        else
        {
            directory = Path.GetDirectoryName(path);
            FilePath = path;
        }

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory ?? throw new InvalidOperationException());

        if (!string.IsNullOrEmpty(FilePath) && !File.Exists(FilePath))
        {
            var fs = File.Create(FilePath);
            fs.Close();
        }

        Name = FileInfo.Name;
    }

    public string FilePath { get; }

    public string Name { get; }

    public bool KeepAlive { get; set; }

    public BlobMetadata? BlobMetadata { get; set; }

    public FileInfo FileInfo => new(FilePath);

    public FileStream FileStream
    {
        get
        {
            lock (_lockObject)
            {
                CloseFileStream();

                if (_disposed)
                    throw new ObjectDisposedException(FilePath);

                if (!FileInfo.Exists)
                    throw new FileNotFoundException(FilePath);

                _stream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                return _stream;
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lockObject)
        {
            try
            {
                CloseFileStream();

                if (KeepAlive)
                    return;

                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    public void Delete()
    {
        KeepAlive = false;
        Dispose();
    }

    ~LocalFile()
    {
        Dispose();
    }

    private void CloseFileStream()
    {
        _stream?.Dispose();
        _stream = null;
    }

    public void Close()
    {
        lock (_lockObject)
        {
            CloseFileStream();
        }
    }

    public async Task<LocalFile> CopyFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        await using var fs = FileStream;
        fs.SetLength(0);
        fs.Position = 0;
        await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        await fs.FlushAsync(cancellationToken).ConfigureAwait(false);
        return this;
    }

    public static async Task<LocalFile> FromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var file = new LocalFile();
        await stream.CopyToAsync(file.FileStream, cancellationToken);
        await file.FileStream.DisposeAsync();
        return file;
    }

    public static async Task<LocalFile> FromStreamAsync(Stream stream, string path, string fileName, CancellationToken cancellationToken = default)
    {
        var pathWithName = Path.Combine(path, $"{fileName}.tmp");
        var file = new LocalFile(pathWithName);

        await stream.CopyToAsync(file.FileStream, cancellationToken);
        await file.FileStream.DisposeAsync();

        return file;
    }

    public static async Task<LocalFile> FromStreamAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var file = FromFileName(fileName);
        await stream.CopyToAsync(file.FileStream, cancellationToken);
        await file.FileStream.DisposeAsync();
        return file;
    }

    public static LocalFile FromFileName(string fileName)
    {
        return new LocalFile(Path.Combine(Path.GetTempPath(), fileName));
    }

    public static LocalFile FromRandomNameWithExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return new LocalFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid()
            .ToString("N") + extension));
    }

    public static LocalFile FromTempFile()
    {
        return new LocalFile();
    }

    public Stream OpenReadStream(bool disposeOwner = true)
    {
        return new LocalFileReadStream(this, disposeOwner);
    }

    #region Read

    public string ReadAllText()
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadAllText(FilePath);
        }
    }

    public Task<string> ReadAllTextAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadAllTextAsync(FilePath, cancellationToken);
        }
    }

    public string[] ReadAllLines()
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadAllLines(FilePath);
        }
    }

    public Task<string[]> ReadAllLinesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadAllLinesAsync(FilePath, cancellationToken);
        }
    }

    public byte[] ReadAllBytes()
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadAllBytes(FilePath);
        }
    }

    public Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadAllBytesAsync(FilePath, cancellationToken);
        }
    }

    public IEnumerable<string> ReadLines()
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadLines(FilePath);
        }
    }

    #endregion

    #region Write

    public void WriteAllText(string content)
    {
        lock (_lockObject)
        {
            CloseFileStream();
            File.WriteAllText(FilePath, content);
        }
    }

    public Task WriteAllTextAsync(string content, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.WriteAllTextAsync(FilePath, content, cancellationToken);
        }
    }

    public void WriteAllLines(IEnumerable<string> contents)
    {
        lock (_lockObject)
        {
            CloseFileStream();
            File.WriteAllLines(FilePath, contents);
        }
    }

    public Task WriteAllLinesAsync(IEnumerable<string> contents, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.WriteAllLinesAsync(FilePath, contents, cancellationToken);
        }
    }

    public void WriteAllBytes(byte[] bytes)
    {
        lock (_lockObject)
        {
            CloseFileStream();
            File.WriteAllBytes(FilePath, bytes);
        }
    }

    public Task WriteAllBytesAsync(byte[] bytes, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.WriteAllBytesAsync(FilePath, bytes, cancellationToken);
        }
    }

    #endregion

    private sealed class LocalFileReadStream : Stream
    {
        private readonly LocalFile _owner;
        private readonly FileStream _stream;
        private readonly bool _disposeOwner;
        private bool _disposed;

        public LocalFileReadStream(LocalFile owner, bool disposeOwner)
        {
            _owner = owner;
            _disposeOwner = disposeOwner;
            _stream = new FileStream(owner.FilePath, new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous
            });
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override void Flush() => _stream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            return _stream.Read(buffer);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _stream.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _stream.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _stream.WriteAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                base.Dispose(disposing);
                return;
            }

            if (disposing)
            {
                _stream.Dispose();

                if (_disposeOwner)
                {
                    _owner.Dispose();
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                await ValueTask.CompletedTask;
                return;
            }

            await _stream.DisposeAsync();

            if (_disposeOwner)
            {
                await _owner.DisposeAsync();
            }

            _disposed = true;
        }
    }
}
