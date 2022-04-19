using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
    private readonly GCPStorageOptions _gcpStorageOptions;

    public GCPStorage(GCPStorageOptions gcpStorageOptions)
    {
        _gcpStorageOptions = gcpStorageOptions;

        _bucket = _gcpStorageOptions.BucketOptions.Bucket;

        if (_gcpStorageOptions.StorageClientBuilder != null)
        {
            _storageClient = _gcpStorageOptions.StorageClientBuilder.Build();
        }
        else if (_gcpStorageOptions.GoogleCredential != null)
        {
            _storageClient = StorageClient.Create(gcpStorageOptions.GoogleCredential);
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

    public async Task<Stream?> DownloadAsStreamAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var stream = new MemoryStream();
            await _storageClient.DownloadObjectAsync(_bucket, blobName, stream, null, cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Stream?> DownloadAsStreamAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
    {
        return await DownloadAsStreamAsync(blobMetadata.Name, cancellationToken);
    }

    public async Task<LocalFile?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var localFile = new LocalFile();

            await _storageClient.DownloadObjectAsync(_bucket, blobName,
                localFile.FileStream, null, cancellationToken);

            return localFile;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<LocalFile?> DownloadAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
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
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
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

    public async Task<BlobMetadata?> GetBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var obj = await _storageClient.GetObjectAsync(_bucket, blobName, null, cancellationToken);

            return new BlobMetadata
            {
                Name = obj.Name,
                Uri = string.IsNullOrEmpty(obj.MediaLink) ? null : new Uri(obj.MediaLink),
                Container = obj.Bucket,
                ContentType = obj.ContentType,
                Length = (long) (obj.Size ?? 0),
            };
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public IAsyncEnumerable<BlobMetadata> GetBlobListAsync(CancellationToken cancellationToken = default)
    {
        return _storageClient.ListObjectsAsync(_bucket, string.Empty,
                new ListObjectsOptions {Projection = Projection.Full})
            .Select(
                x => new BlobMetadata
                {
                    Name = x.Name,
                    Uri = string.IsNullOrEmpty(x.MediaLink) ? null : new Uri(x.MediaLink),
                    Container = x.Bucket,
                    ContentType = x.ContentType,
                    Length = (long) (x.Size ?? 0),
                }
            );
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobsAsync(IEnumerable<string> blobNames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blobName in blobNames)
        {
            var blobMetadata = await GetBlobAsync(blobName, cancellationToken);

            if (blobMetadata is not null)
            {
                yield return blobMetadata;
            }
        }
    }

    #endregion

    #region Upload

    public async Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobName, new MemoryStream(Encoding.UTF8.GetBytes(content)), null, cancellationToken);
    }

    public async Task UploadStreamAsync(string blobName, Stream dataStream, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobName, dataStream, null, cancellationToken);
    }

    public async Task UploadFileAsync(string blobName, string pathToFile, CancellationToken cancellationToken = default)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            await UploadStreamInternalAsync(blobName, fs, null, cancellationToken);
        }
    }

    public async Task UploadStreamAsync(BlobMetadata blobMetadata, Stream dataStream, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobMetadata.Name, dataStream, blobMetadata.ContentType, cancellationToken);
    }

    public async Task UploadFileAsync(BlobMetadata blobMetadata, string pathToFile, CancellationToken cancellationToken = default)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            await UploadStreamInternalAsync(blobMetadata.Name, fs, blobMetadata.ContentType, cancellationToken);
        }
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, string content, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobMetadata.Name, new MemoryStream(Encoding.UTF8.GetBytes(content)), blobMetadata.ContentType,
            cancellationToken);
    }

    public async Task UploadAsync(BlobMetadata blobMetadata, byte[] data, CancellationToken cancellationToken = default)
    {
        await UploadStreamInternalAsync(blobMetadata.Name, new MemoryStream(data), blobMetadata.ContentType, cancellationToken);
    }

    public async Task<string> UploadAsync(string content, CancellationToken cancellationToken = default)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        await UploadStreamInternalAsync(fileName, new MemoryStream(Encoding.UTF8.GetBytes(content)), null, cancellationToken);

        return fileName;
    }

    public async Task<string> UploadAsync(Stream dataStream, CancellationToken cancellationToken = default)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        await UploadStreamInternalAsync(fileName, dataStream, null, cancellationToken);

        return fileName;
    }


    private async Task UploadStreamInternalAsync(string blobName, Stream dataStream, string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        contentType ??= Constants.ContentType;

        try
        {
            await _storageClient.UploadObjectAsync(_bucket, blobName, contentType, dataStream, null, cancellationToken);
        }
        catch
        {
            await CreateContainerAsync(cancellationToken);
            await _storageClient.UploadObjectAsync(_bucket, blobName, contentType, dataStream, null, cancellationToken);
        }
    }

    #endregion

    #region CreateContainer

    public async Task CreateContainerAsync(CancellationToken cancellationToken = default)
    {
        if (_gcpStorageOptions.OriginalOptions != null)
        {
            await _storageClient.CreateBucketAsync(_gcpStorageOptions.BucketOptions.ProjectId, _bucket, _gcpStorageOptions.OriginalOptions,
                cancellationToken);
        }
        else
        {
            await _storageClient.CreateBucketAsync(_gcpStorageOptions.BucketOptions.ProjectId, _bucket, cancellationToken: cancellationToken);
        }
    }

    #endregion

    public async Task SetLegalHoldAsync(string blobName, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        var storageObject = await _storageClient.GetObjectAsync(_bucket, blobName, cancellationToken: cancellationToken);
        storageObject.TemporaryHold = hasLegalHold;

        await _storageClient.UpdateObjectAsync(storageObject, cancellationToken: cancellationToken);
    }

    public async Task<bool> HasLegalHoldAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var storageObject = await _storageClient.GetObjectAsync(_bucket, blobName, cancellationToken: cancellationToken);

        return storageObject.TemporaryHold ?? false;
    }
}