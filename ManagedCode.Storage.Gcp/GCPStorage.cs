using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Gcp.Options;

namespace ManagedCode.Storage.Gcp;

public class GCPStorage : IGCPStorage
{
    private readonly string _bucket;
    private readonly StorageClient _storageClient;

    public GCPStorage(GCPStorageOptions gcpStorageOptions)
    {
        _bucket = gcpStorageOptions.BucketOptions.Bucket;

        if (gcpStorageOptions.StorageClientBuilder != null)
        {
            _storageClient = gcpStorageOptions.StorageClientBuilder.Build();
        }
        else if (gcpStorageOptions.GoogleCredential != null)
        {
            _storageClient = StorageClient.Create(gcpStorageOptions.GoogleCredential);
        }

        try
        {
            if (gcpStorageOptions.OriginalOptions != null)
            {
                _storageClient.CreateBucket(gcpStorageOptions.BucketOptions.ProjectId, _bucket, gcpStorageOptions.OriginalOptions);
            }
            else
            {
                _storageClient.CreateBucket(gcpStorageOptions.BucketOptions.ProjectId, _bucket);
            }
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _storageClient.Dispose();
    }

    #region Delete

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        await _storageClient.DeleteObjectAsync(_bucket, blobName, null, cancellationToken);
    }

    public async Task DeleteAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
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

    public async Task<Stream> DownloadAsStreamAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var stream = new MemoryStream();
        await _storageClient.DownloadObjectAsync(_bucket, blobName, stream, null, cancellationToken);

        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }

    public async Task<Stream> DownloadAsStreamAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsStreamAsync(blobMetadata.Name, cancellationToken);
    }

    public async Task<LocalFile> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var localFile = new LocalFile();

        await _storageClient.DownloadObjectAsync(_bucket, blobName,
            localFile.FileStream, null, cancellationToken);

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
        try
        {
            await _storageClient.GetObjectAsync(_bucket, blobName, null, cancellationToken);

            return true;
        }
        catch (GoogleApiException)
        {
            return false;
        }
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

    public async Task<BlobMetadata> GetBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var obj = await _storageClient.GetObjectAsync(_bucket, blobName, null, cancellationToken);

        return new BlobMetadata
        {
            Name = obj.Name,
            Uri = new Uri(obj.MediaLink)
        };
    }

    public IAsyncEnumerable<BlobMetadata> GetBlobListAsync(CancellationToken cancellationToken = default)
    {
        return _storageClient.ListObjectsAsync(_bucket, string.Empty,
                new ListObjectsOptions {Projection = Projection.Full})
            .Select(
                x => new BlobMetadata
                {
                    Name = x.Name,
                    Uri = new Uri(x.MediaLink)
                }
            );
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobsAsync(IEnumerable<string> blobNames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            yield return await GetBlobAsync(blob, cancellationToken);
        }
    }

    #endregion

    #region Upload

    public async Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default)
    {
        await _storageClient.UploadObjectAsync(_bucket, blobName, Constants.ContentType, new MemoryStream(Encoding.UTF8.GetBytes(content)), null,
            cancellationToken);
    }

    public async Task UploadStreamAsync(string blobName, Stream dataStream, CancellationToken cancellationToken = default)
    {
        await _storageClient.UploadObjectAsync(_bucket, blobName, Constants.ContentType, dataStream, null, cancellationToken);
    }

    public async Task UploadFileAsync(string blobName, string pathToFile, CancellationToken cancellationToken = default)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            await UploadStreamAsync(blobName, fs, cancellationToken);
        }
    }

    public async Task UploadStreamAsync(BlobMetadata blobMetadata, Stream dataStream, CancellationToken cancellationToken = default)
    {
        await _storageClient.UploadObjectAsync(_bucket, blobMetadata.Name, blobMetadata.ContentType, dataStream, null, cancellationToken);
    }

    public async Task UploadFileAsync(BlobMetadata blobMetadata, string pathToFile, CancellationToken cancellationToken = default)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            await UploadStreamAsync(blobMetadata, fs, cancellationToken);
        }
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, string content, CancellationToken cancellationToken = default)
    {
        await _storageClient.UploadObjectAsync(_bucket, blobMetadata.Name, blobMetadata.ContentType,
            new MemoryStream(Encoding.UTF8.GetBytes(content)), null,
            cancellationToken);
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, byte[] data, CancellationToken cancellationToken = default)
    {
        await _storageClient.UploadObjectAsync(_bucket, blobMetadata.Name, blobMetadata.ContentType, new MemoryStream(data), null, cancellationToken);
    }

    public async Task<string> UploadAsync(string content, CancellationToken cancellationToken = default)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        await UploadAsync(fileName, content, cancellationToken);

        return fileName;
    }

    public async Task<string> UploadAsync(Stream dataStream, CancellationToken cancellationToken = default)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        await UploadStreamAsync(fileName, dataStream, cancellationToken);

        return fileName;
    }

    #endregion
}