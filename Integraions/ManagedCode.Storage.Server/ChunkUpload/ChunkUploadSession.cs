using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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

    public SemaphoreSlim Gate { get; } = new(1, 1);

    public bool Closed { get; set; }

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
        var expectedCount = TotalChunks > 0 ? TotalChunks : _chunkFiles.Count;
        for (var index = 0; index < expectedCount; index++)
        {
            var chunkIndex = index + 1;
            if (!_chunkFiles.ContainsKey(chunkIndex))
            {
                throw new InvalidOperationException($"Missing chunk {chunkIndex} for upload {UploadId}");
            }
        }
    }

    public void Cleanup(bool keepMergedFile = false)
    {
        foreach (var (_, path) in _chunkFiles)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        if (!keepMergedFile && Directory.Exists(WorkingDirectory))
        {
            Directory.Delete(WorkingDirectory, recursive: true);
        }
    }
}
