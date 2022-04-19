using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.FileSystem;

public class FileSystemStorage : IFileSystemStorage
{
    private readonly string _path;
    private readonly Dictionary<string, FileStream> _lockedFiles = new();

    public FileSystemStorage(FileSystemStorageOptions fileSystemStorageOptions)
    {
        _path = fileSystemStorageOptions?.BaseFolder ?? Environment.CurrentDirectory;
        EnsureDirectoryExists();
    }

    public void Dispose()
    {
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_path))
        {
            Directory.CreateDirectory(_path);
        }
    }

    #region Delete

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        await Task.Yield();

        var filePath = Path.Combine(_path, blobName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public async Task DeleteAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();
        await DeleteAsync(blobMetadata.Name, cancellationToken);
    }

    public async Task DeleteAsync(IEnumerable<string> blobNames, CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            await DeleteAsync(blob, cancellationToken);
        }
    }

    public async Task DeleteAsync(IEnumerable<BlobMetadata> blobNames, CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            await DeleteAsync(blob, cancellationToken);
        }
    }

    #endregion

    #region Download

    public async Task<Stream?> DownloadAsStreamAsync(string blobName, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var filePath = Path.Combine(_path, blobName);

        return File.Exists(filePath) ? new FileStream(filePath, FileMode.Open, FileAccess.Read) : null;
    }

    public async Task<Stream?> DownloadAsStreamAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsStreamAsync(blobMetadata.Name, cancellationToken);
    }

    public async Task<LocalFile?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var filePath = Path.Combine(_path, blobName);

        return File.Exists(filePath) ? new LocalFile(filePath) : null;
    }

    public async Task<LocalFile?> DownloadAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsync(blobMetadata.Name, cancellationToken);
    }

    #endregion

    #region Exists

    public async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        await Task.Yield();

        var filePath = Path.Combine(_path, blobName);

        return File.Exists(filePath);
    }

    public async Task<bool> ExistsAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await ExistsAsync(blobMetadata.Name, cancellationToken);
    }

    public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobNames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            yield return await ExistsAsync(blob, cancellationToken);
        }
    }

    public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<BlobMetadata> blobs,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobs)
        {
            yield return await ExistsAsync(blob, cancellationToken);
        }
    }

    #endregion

    #region Get

    public Task<BlobMetadata?> GetBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        var fileInfo = new FileInfo(Path.Combine(_path, blobName));

        if (fileInfo.Exists)
        {
            var result = new BlobMetadata
            {
                Name = fileInfo.Name,
                Uri = new Uri(Path.Combine(_path, blobName)),
                ContentType = MimeHelper.GetMimeType(fileInfo.Extension),
                Length = fileInfo.Length
            };

            return Task.FromResult<BlobMetadata?>(result);
        }

        return Task.FromResult<BlobMetadata?>(null);
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in Directory.EnumerateFiles(_path))
        {
            var blobMetadata = await GetBlobAsync(file, cancellationToken);

            if (blobMetadata is not null)
            {
                yield return blobMetadata;
            }
        }
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobsAsync(IEnumerable<string> blobNames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in blobNames)
        {
            var blobMetadata = await GetBlobAsync(file, cancellationToken);

            if (blobMetadata is not null)
            {
                yield return blobMetadata;
            }
        }
    }

    #endregion

    #region Upload

    public async Task UploadStreamAsync(string blobName, Stream dataStream, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();
        var filePath = Path.Combine(_path, blobName);

        using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
        {
            dataStream.Seek(0, SeekOrigin.Begin);
            await dataStream.CopyToAsync(fs, 81920, cancellationToken);
        }
    }

    public async Task UploadFileAsync(string blobName, string pathToFile, CancellationToken cancellationToken = default)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            await UploadStreamAsync(blobName, fs, cancellationToken);
        }
    }

    public async Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();
        var filePath = Path.Combine(_path, blobName);

        await Task.Run(() => File.WriteAllText(filePath, content), cancellationToken);
    }

    public Task UploadStreamAsync(BlobMetadata blobMetadata, Stream dataStream, CancellationToken cancellationToken = default)
    {
        return UploadStreamAsync(blobMetadata.Name, dataStream, cancellationToken);
    }

    public Task UploadFileAsync(BlobMetadata blobMetadata, string pathToFile, CancellationToken cancellationToken = default)
    {
        return UploadFileAsync(blobMetadata.Name, pathToFile, cancellationToken);
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, string content, CancellationToken cancellationToken = default)
    {
        await UploadAsync(blobMetadata.Name, content, cancellationToken);
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, byte[] data, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();
        var filePath = Path.Combine(_path, blobMetadata.Name);

        await Task.Run(() => File.WriteAllBytes(filePath, data), cancellationToken);
    }

    public async Task<string> UploadAsync(string content, CancellationToken cancellationToken = default)
    {
        string fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        await UploadAsync(fileName, content, cancellationToken);

        return fileName;
    }

    public async Task<string> UploadAsync(Stream dataStream, CancellationToken cancellationToken = default)
    {
        string fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        await UploadStreamAsync(fileName, dataStream, cancellationToken);

        return fileName;
    }

    #endregion

    #region CreateContainer

    public async Task CreateContainerAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        if (!Directory.Exists(_path))
        {
            Directory.CreateDirectory(_path);
        }
    }

    #endregion

    public async Task SetLegalHoldAsync(string blobName, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        if (hasLegalHold && !_lockedFiles.ContainsKey(blobName))
        {
            var file = await DownloadAsync(blobName, cancellationToken);

            if (file is null) return;

            var fileStream = File.OpenRead(file.FilePath); // Opening with FileAccess.Read only
            fileStream.Lock(0, fileStream.Length); // Attempting to lock a region of the read-only file

            _lockedFiles.Add(blobName, fileStream);

            return;
        }

        if (!hasLegalHold)
        {
            if (_lockedFiles.ContainsKey(blobName))
            {
                _lockedFiles[blobName].Unlock(0, _lockedFiles[blobName].Length);
                _lockedFiles[blobName].Dispose();
                _lockedFiles.Remove(blobName);
            }
        }
    }

    public Task<bool> HasLegalHoldAsync(string blobName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_lockedFiles.ContainsKey(blobName));
    }
}