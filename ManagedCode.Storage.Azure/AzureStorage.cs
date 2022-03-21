using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Azure;

public class AzureStorage : IAzureStorage
{
    private readonly BlobContainerClient _blobContainerClient;

    public AzureStorage(AzureStorageOptions options)
    {
        _blobContainerClient = new BlobContainerClient(
            options.ConnectionString,
            options.Container,
            options.OriginalOptions
        );

        if (options.ShouldCreateIfNotExists)
        {
            _blobContainerClient.CreateIfNotExists(PublicAccessType.BlobContainer);
        }

        _blobContainerClient.SetAccessPolicy(options.PublicAccessType);
    }

    public void Dispose()
    {
    }

    #region Delete

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        await blobClient.DeleteAsync(DeleteSnapshotsOption.None, null, cancellationToken);
    }

    public async Task DeleteAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobMetadata.Name);
        await blobClient.DeleteAsync(DeleteSnapshotsOption.None, null, cancellationToken);
    }

    public async Task DeleteAsync(IEnumerable<string> blobNames, CancellationToken cancellationToken = default)
    {
        foreach (var blobName in blobNames)
        {
            await DeleteAsync(blobName, cancellationToken);
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

    public async Task<Stream> DownloadAsStreamAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        var res = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

        return res.Value.Content;
    }

    public async Task<Stream> DownloadAsStreamAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsStreamAsync(blobMetadata.Name, cancellationToken);
    }

    public async Task<LocalFile> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        var localFile = new LocalFile();

        await blobClient.DownloadToAsync(localFile.FileStream, cancellationToken);

        return localFile;
    }

    public async Task<LocalFile> DownloadAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsync(blobMetadata.Name, cancellationToken);
    }

    #endregion

    #region Exists

    public async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        return await blobClient.ExistsAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobMetadata.Name);

        return await blobClient.ExistsAsync(cancellationToken);
    }

    public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobNames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob);
            yield return await blobClient.ExistsAsync(cancellationToken);
        }
    }

    public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<BlobMetadata> blobs,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobs)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob.Name);
            yield return await blobClient.ExistsAsync(cancellationToken);
        }
    }

    #endregion

    #region Get

    public async Task<BlobMetadata> GetBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        return new BlobMetadata
        {
            Name = blobClient.Name,
            Uri = blobClient.Uri
        };
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobsAsync(IEnumerable<string> blobNames, CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            yield return await GetBlobAsync(blob, cancellationToken);
        }
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobListAsync(CancellationToken cancellationToken = default)
    {
        var blobs = _blobContainerClient.GetBlobsAsync();

        await foreach (var item in blobs.AsPages())
        {
            foreach (var blobItem in item.Values)
            {
                yield return new BlobMetadata
                {
                    Name = blobItem.Name,
                };
            }
        }
    }

    #endregion

    #region Upload

    public async Task UploadStreamAsync(string blobName, Stream dataStream, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(dataStream, cancellationToken);
    }

    public async Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(BinaryData.FromString(content), cancellationToken);
    }

    public async Task UploadFileAsync(string blobName, string pathToFile, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            await blobClient.UploadAsync(fs, cancellationToken);
        }
    }

    public async Task UploadStreamAsync(BlobMetadata blobMetadata, Stream dataStream, CancellationToken cancellationToken = default)
    {
        await UploadStreamAsync(blobMetadata.Name, dataStream, cancellationToken);
    }

    public async Task UploadFileAsync(BlobMetadata blobMetadata, string pathToFile, CancellationToken cancellationToken = default)
    {
        await UploadFileAsync(blobMetadata.Name, pathToFile, cancellationToken);
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, string content, CancellationToken cancellationToken = default)
    {
        await UploadAsync(blobMetadata.Name, content, cancellationToken);
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, byte[] data, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobMetadata.Name);
        await blobClient.UploadAsync(BinaryData.FromBytes(data), cancellationToken);
    }

    public async Task<string> UploadAsync(string content, CancellationToken cancellationToken = default)
    {
        string fileName = $"{Guid.NewGuid().ToString("N").ToLowerInvariant()}.txt";

        var blobClient = _blobContainerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(BinaryData.FromString(content), cancellationToken);

        return fileName;
    }

    public async Task<string> UploadAsync(Stream dataStream, CancellationToken cancellationToken = default)
    {
        string fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();

        var blobClient = _blobContainerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(dataStream, cancellationToken);

        return fileName;
    }

    #endregion
}