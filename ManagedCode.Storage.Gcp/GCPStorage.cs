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

        _bucket = _gcpStorageOptions.BucketOptions?.Bucket!;

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

    #region Async

    #region CreateContainer

    public async Task CreateContainerAsync(CancellationToken cancellationToken = default)
    {
        if (_gcpStorageOptions.OriginalOptions != null)
        {
            await _storageClient.CreateBucketAsync(_gcpStorageOptions.BucketOptions?.ProjectId, _bucket, _gcpStorageOptions.OriginalOptions,
                cancellationToken);
        }
        else
        {
            await _storageClient.CreateBucketAsync(_gcpStorageOptions.BucketOptions?.ProjectId, _bucket, cancellationToken: cancellationToken);
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

    #region LegalHold

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

    #endregion

    #endregion

    #region Sync

    #region CreateContainer

    public void CreateContainer()
    {
        if (_gcpStorageOptions.OriginalOptions != null)
        {
            _storageClient.CreateBucket(_gcpStorageOptions.BucketOptions?.ProjectId, _bucket, _gcpStorageOptions.OriginalOptions);
        }
        else
        {
            _storageClient.CreateBucket(_gcpStorageOptions.BucketOptions?.ProjectId, _bucket);
        }
    }

    #endregion

    #region Get

    public BlobMetadata? GetBlob(string blobName)
    {
        try
        {
            var obj = _storageClient.GetObject(_bucket, blobName);

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

    public IEnumerable<BlobMetadata> GetBlobList()
    {
        return _storageClient.ListObjects(_bucket, string.Empty,
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

    public IEnumerable<BlobMetadata> GetBlobs(IEnumerable<string> blobNames)
    {
        foreach (var blobName in blobNames)
        {
            var blobMetadata = GetBlob(blobName);

            if (blobMetadata is not null)
            {
                yield return blobMetadata;
            }
        }
    }

    #endregion

    #region Upload

    public void Upload(string blobName, string content)
    {
        UploadStreamInternal(blobName, new MemoryStream(Encoding.UTF8.GetBytes(content)));
    }

    public void UploadStream(string blobName, Stream dataStream)
    {
        UploadStreamInternal(blobName, dataStream);
    }

    public void UploadFile(string blobName, string pathToFile)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            UploadStreamInternal(blobName, fs);
        }
    }

    public void UploadStream(BlobMetadata blobMetadata, Stream dataStream)
    {
        UploadStreamInternal(blobMetadata.Name, dataStream, blobMetadata.ContentType);
    }

    public void UploadFile(BlobMetadata blobMetadata, string pathToFile)
    {
        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            UploadStreamInternal(blobMetadata.Name, fs, blobMetadata.ContentType);
        }
    }

    public void Upload(BlobMetadata blobMetadata, string content)
    {
        UploadStreamInternal(blobMetadata.Name, new MemoryStream(Encoding.UTF8.GetBytes(content)), blobMetadata.ContentType);
    }

    public void Upload(BlobMetadata blobMetadata, byte[] data)
    {
        UploadStreamInternal(blobMetadata.Name, new MemoryStream(data), blobMetadata.ContentType);
    }

    public string Upload(string content)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        UploadStreamInternal(fileName, new MemoryStream(Encoding.UTF8.GetBytes(content)));

        return fileName;
    }

    public string Upload(Stream dataStream)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        UploadStreamInternal(fileName, dataStream);

        return fileName;
    }

    private void UploadStreamInternal(string blobName, Stream dataStream, string? contentType = null)
    {
        contentType ??= Constants.ContentType;

        try
        {
            _storageClient.UploadObject(_bucket, blobName, contentType, dataStream);
        }
        catch
        {
            CreateContainer();
            _storageClient.UploadObject(_bucket, blobName, contentType, dataStream);
        }
    }

    #endregion

    #region Download

    public Stream? DownloadAsStream(string blobName)
    {
        try
        {
            var stream = new MemoryStream();
            _storageClient.DownloadObjectAsync(_bucket, blobName, stream);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Stream? DownloadAsStream(BlobMetadata blobMetadata)
    {
        return DownloadAsStream(blobMetadata.Name);
    }

    public LocalFile? Download(string blobName)
    {
        try
        {
            var localFile = new LocalFile();

            _storageClient.DownloadObject(_bucket, blobName,
                localFile.FileStream);

            return localFile;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public LocalFile? Download(BlobMetadata blobMetadata)
    {
        return Download(blobMetadata.Name);
    }

    #endregion

    #region Delete

    public void Delete(string blobName)
    {
        _storageClient.DeleteObject(_bucket, blobName);
    }

    public void Delete(BlobMetadata blobMetadata)
    {
        Delete(blobMetadata.Name);
    }

    public void Delete(IEnumerable<string> blobNames)
    {
        foreach (var blob in blobNames)
        {
            Delete(blob);
        }
    }

    public void Delete(IEnumerable<BlobMetadata> blobNames)
    {
        foreach (var blob in blobNames)
        {
            Delete(blob);
        }
    }

    #endregion

    #region Exists

    public bool Exists(string blobName)
    {
        try
        {
            _storageClient.GetObject(_bucket, blobName);

            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public bool Exists(BlobMetadata blobMetadata)
    {
        return Exists(blobMetadata.Name);
    }

    public IEnumerable<bool> Exists(IEnumerable<string> blobNames)
    {
        foreach (var blob in blobNames)
        {
            yield return Exists(blob);
        }
    }

    public IEnumerable<bool> Exists(IEnumerable<BlobMetadata> blobs)
    {
        foreach (var blob in blobs)
        {
            yield return Exists(blob);
        }
    }

    #endregion

    #region LegalHold

    public void SetLegalHold(string blobName, bool hasLegalHold)
    {
        var storageObject = _storageClient.GetObject(_bucket, blobName);
        storageObject.TemporaryHold = hasLegalHold;

        _storageClient.UpdateObject(storageObject);
    }

    public bool HasLegalHold(string blobName)
    {
        var storageObject = _storageClient.GetObject(_bucket, blobName);

        return storageObject.TemporaryHold ?? false;
    }

    #endregion

    #endregion
}