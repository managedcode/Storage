using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.FileSystem;

public class FileSystemStorage : IFileSystemStorage
{
    private readonly string _path;

    public FileSystemStorage(FSStorageOptions fsStorageOptions)
    {
        _path = Path.Combine(fsStorageOptions.CommonPath, fsStorageOptions.Path);
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

    public async Task DeleteAsync(string blob, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        await Task.Yield();

        var filePath = Path.Combine(_path, blob);

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

    public async Task DeleteAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobs)
        {
            await DeleteAsync(blob, cancellationToken);
        }
    }

    public async Task DeleteAsync(IEnumerable<BlobMetadata> blobs, CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobs)
        {
            await DeleteAsync(blob, cancellationToken);
        }
    }

    #endregion

    #region Download

    public async Task<Stream> DownloadAsStreamAsync(string blob, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();
        var memoryStream = new MemoryStream();
        var filePath = Path.Combine(_path, blob);

        if (File.Exists(filePath))
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await fs.CopyToAsync(memoryStream, 81920, cancellationToken);
            }

            return memoryStream;
        }

        return memoryStream;
    }

    public async Task<Stream> DownloadAsStreamAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsStreamAsync(blobMetadata.Name, cancellationToken);
    }

    public async Task<LocalFile> DownloadAsync(string blob, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        var localFile = new LocalFile();
        var filePath = Path.Combine(_path, blob);

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            //TODO: Temporary added bufferSize
            await fs.CopyToAsync(localFile.FileStream, 1024, cancellationToken);
        }

        return localFile;
    }

    public async Task<LocalFile> DownloadAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsync(blobMetadata.Name, cancellationToken);
    }

    #endregion

    #region Exists

    public async Task<bool> ExistsAsync(string blob, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        await Task.Yield();

        var filePath = Path.Combine(_path, blob);

        return File.Exists(filePath);
    }

    public async Task<bool> ExistsAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await ExistsAsync(blobMetadata.Name, cancellationToken);
    }

    public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobs,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobs)
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

    public Task<BlobMetadata> GetBlobAsync(string blob, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        var fileInfo = new FileInfo(Path.Combine(_path, blob));

        if (fileInfo.Exists)
        {
            var result = new BlobMetadata
            {
                Name = fileInfo.Name,
                Uri = new Uri(Path.Combine(_path, blob))
            };
            return Task.FromResult(result);
        }

        return null;
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in Directory.EnumerateFiles(_path))
        {
            yield return await GetBlobAsync(file, cancellationToken);
        }
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobsAsync(IEnumerable<string> blobs,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in blobs)
        {
            yield return await GetBlobAsync(file, cancellationToken);
        }
    }

    #endregion

    #region Upload

    public async Task UploadStreamAsync(string blob, Stream dataStream, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();
        var filePath = Path.Combine(_path, blob);

        using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
        {
            dataStream.Seek(0, SeekOrigin.Begin);
            await dataStream.CopyToAsync(fs, 81920, cancellationToken);
        }
    }

    public async Task UploadFileAsync(string blob, string pathToFile = null, CancellationToken cancellationToken = default)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            await UploadStreamAsync(blob, fs, cancellationToken);
        }
    }

    public async Task UploadAsync(string blob, string content, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();
        var filePath = Path.Combine(_path, blob);

        //TODO: Need fix
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

        //TODO: Need fix
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
}