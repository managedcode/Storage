using System;
using System.IO;
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
            {
                FilePath = Path.Combine(directory, $"{name}.tmp");
            }
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

    public string ReadAllText()
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadAllText(FilePath);
        }
    }

    public Task<string> ReadAllTextAsync()
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadAllTextAsync(FilePath);
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

    public Task<string[]> ReadAllLinesAsync()
    {
        lock (_lockObject)
        {
            CloseFileStream();
            return File.ReadAllLinesAsync(FilePath);
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

    public static async Task<LocalFile> FromStreamAsync(Stream stream, string fileName)
    {
        var file = FromFileName(fileName);
        await stream.CopyToAsync(file.FileStream);
        await file.FileStream.DisposeAsync();

        return file;
    }

    public static LocalFile FromFileName(string fileName)
    {
        return new LocalFile(Path.Combine(Path.GetTempPath(), fileName));
    }

    public static LocalFile FromTempFile()
    {
        return new LocalFile();
    }
}