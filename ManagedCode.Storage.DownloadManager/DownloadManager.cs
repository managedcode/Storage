using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.DownloadManager.Helpers;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.DownloadManager;

public class DownloadManager : IDownloadManager
{
    private readonly IStorage _storage;

    public DownloadManager(IStorage storage)
    {
        _storage = storage;
    }

    public Task<Stream> DownloadAsStreamAsync(string blob)
    {
        return _storage.DownloadAsStreamAsync(blob);
    }

    public async Task UploadStreamAsync(string fileName, Stream stream, CancellationToken cancellationToken = default)
    {
        var (fileInfo, fileStream) = await FileSaver.SaveTemporaryFile(stream);
        await _storage.UploadStreamAsync(fileName, fileStream, cancellationToken);

        fileInfo.Delete();
    }

    public async Task UploadIFormFileAsync(string fileName, IFormFile formFile, CancellationToken cancellationToken = default)
    {
        var (fileInfo, fileStream) = await FileSaver.SaveTemporaryFile(formFile);
        await _storage.UploadStreamAsync(fileName, fileStream, cancellationToken);

        fileInfo.Delete();
    }

    public async Task UploadStreamAsync(BlobMetadata blobMetadata, Stream stream, CancellationToken cancellationToken = default)
    {
        var (fileInfo, fileStream) = await FileSaver.SaveTemporaryFile(stream);
        await _storage.UploadStreamAsync(blobMetadata, fileStream, cancellationToken);

        fileInfo.Delete();
    }

    public async Task UploadIFormFileAsync(BlobMetadata blobMetadata, IFormFile formFile, CancellationToken cancellationToken = default)
    {
        var (fileInfo, fileStream) = await FileSaver.SaveTemporaryFile(formFile);
        await _storage.UploadStreamAsync(blobMetadata, fileStream, cancellationToken);

        fileInfo.Delete();
    }

    public Task UploadAsync(BlobMetadata blobMetadata, string content, CancellationToken cancellationToken = default)
    {
        return _storage.UploadAsync(blobMetadata, content, cancellationToken);
    }

    public Task UploadAsync(BlobMetadata blobMetadata, byte[] data, CancellationToken cancellationToken = default)
    {
        return _storage.UploadAsync(blobMetadata, data, cancellationToken);
    }

    public Task UploadFileAsync(BlobMetadata blobMetadata, string pathToFile, CancellationToken cancellationToken = default)
    {
        return _storage.UploadFileAsync(blobMetadata, pathToFile, cancellationToken);
    }

    public Task UploadFileAsync(string fileName, string pathToFile, CancellationToken cancellationToken = default)
    {
        return _storage.UploadFileAsync(fileName, pathToFile, cancellationToken);
    }
}