using System;
using System.IO;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Core.Models;

public class LocalFile : IDisposable, IAsyncDisposable
{
    private readonly object _lockObject = new();
    private bool _disposed;
    private FileStream? _stream;

    public LocalFile(bool keepAlive = false) : this(Path.GetTempFileName(), keepAlive)
    {
    }

    public LocalFile(string path, bool keepAlive = false)
    {
        string? directory;
        KeepAlive = keepAlive;

        if (string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            directory = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);
            FilePath = Path.Combine(directory, $"{name}.tmp");
        }
        else
        {
            directory = Path.GetDirectoryName(path);
            FilePath = path;
        }

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory ?? throw new InvalidOperationException());
        }

        if (!File.Exists(FilePath))
        {
            var fs = File.Create(FilePath);
            fs.Close();
        }

        FileName = FileInfo.Name;
    }

    public string FilePath { get; }

    public string FileName { get; }

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
                {
                    throw new ObjectDisposedException(FilePath);
                }

                if (!FileInfo.Exists)
                {
                    throw new FileNotFoundException(FilePath);
                }

                _stream = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite);
                return _stream;
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(Task.Run(Dispose));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lockObject)
        {
            try
            {
                CloseFileStream();

                if (KeepAlive)
                {
                    return;
                }

                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            finally
            {
                _disposed = true;
            }
        }
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

    public async Task<LocalFile> CopyFromStreamAsync(Stream stream)
    {
        await stream.CopyToAsync(FileStream);
        FileStream.Dispose();
        return this;
    }

    public static async Task<LocalFile> FromStreamAsync(Stream stream)
    {
        var file = new LocalFile();
        await stream.CopyToAsync(file.FileStream);
        file.FileStream.Dispose();

        return file;
    }
}