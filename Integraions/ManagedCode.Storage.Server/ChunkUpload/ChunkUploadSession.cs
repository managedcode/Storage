using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace ManagedCode.Storage.Server.ChunkUpload;

internal sealed class ChunkUploadSession
{
    private readonly ConcurrentDictionary<int, string> _chunkFiles = new();

    public ChunkUploadSession(string uploadId, string fileName, string? contentType, int totalChunks, int chunkSize, long? fileSize, string workingDirectory)
    {
        UploadId = uploadId;
        FileName = fileName;
        ContentType = contentType;
        TotalChunks = totalChunks;
        ChunkSize = chunkSize;
        FileSize = fileSize;
        WorkingDirectory = workingDirectory;
        LastTouchedUtc = DateTimeOffset.UtcNow;
    }

    public string UploadId { get; }

    public string FileName { get; }

    public string? ContentType { get; }

    public int TotalChunks { get; }

    public int ChunkSize { get; }

    public long? FileSize { get; }

    public string WorkingDirectory { get; }

    public DateTimeOffset LastTouchedUtc { get; private set; }

    public IReadOnlyDictionary<int, string> ChunkFiles => _chunkFiles;

    public void Touch()
    {
        LastTouchedUtc = DateTimeOffset.UtcNow;
    }

    public string RegisterChunk(int index, string path)
    {
        _chunkFiles[index] = path;
        Touch();
        return path;
    }

    public void EnsureAllChunksPresent()
    {
        if (TotalChunks <= 0)
        {
            return;
        }

        for (var i = 1; i <= TotalChunks; i++)
        {
            if (!_chunkFiles.ContainsKey(i))
            {
                throw new InvalidOperationException($"Missing chunk {i} for upload {UploadId}");
            }
        }
    }

    public void Cleanup()
    {
        foreach (var (_, path) in _chunkFiles)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        if (Directory.Exists(WorkingDirectory))
        {
            Directory.Delete(WorkingDirectory, recursive: true);
        }
    }
}
