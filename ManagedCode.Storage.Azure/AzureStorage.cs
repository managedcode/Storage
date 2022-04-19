using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Azure;

public class AzureStorage : IAzureStorage
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly AzureStorageOptions _options;

    public AzureStorage(AzureStorageOptions options)
    {
        _options = options;

        _blobContainerClient = new BlobContainerClient(
            options.ConnectionString,
            options.Container,
            options.OriginalOptions
        );
    }

    public void Dispose()
    {
    }

    #region Async

    #region CreateContainer

    public async Task CreateContainerAsync(CancellationToken cancellationToken = default)
    {
        if (_options.ShouldCreateIfNotExists)
        {
            await _blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer, cancellationToken: cancellationToken);
        }

        await _blobContainerClient.SetAccessPolicyAsync(_options.PublicAccessType, cancellationToken: cancellationToken);
    }

    #endregion

    #region Get

    public async Task<BlobMetadata?> GetBlobAsync(string blobName, CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

        return new BlobMetadata
        {
            Name = blobClient.Name,
            Uri = blobClient.Uri,
            Container = blobClient.BlobContainerName,
            Length = properties.Value.ContentLength
        };
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobsAsync(IEnumerable<string> blobNames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var blob in blobNames)
        {
            var blobMetadata = await GetBlobAsync(blob, cancellationToken);

            if (blobMetadata is not null)
            {
                yield return blobMetadata;
            }
        }
    }

    public async IAsyncEnumerable<BlobMetadata> GetBlobListAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var blobs = _blobContainerClient.GetBlobsAsync(cancellationToken: cancellationToken);

        await foreach (var item in blobs.AsPages())
        {
            foreach (var blobItem in item.Values)
            {
                var blobMetadata = new BlobMetadata
                {
                    Name = blobItem.Name,
                    Length = blobItem.Properties.ContentLength ?? 0
                };

                yield return blobMetadata;
            }
        }
    }

    #endregion

    #region Upload

    public async Task UploadStreamAsync(string blobName, Stream dataStream, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        try
        {
            await blobClient.UploadAsync(dataStream, cancellationToken);
        }
        catch (RequestFailedException)
        {
            await CreateContainerAsync(cancellationToken);
            await blobClient.UploadAsync(dataStream, cancellationToken);
        }
    }

    public async Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        try
        {
            await blobClient.UploadAsync(BinaryData.FromString(content), cancellationToken);
        }
        catch (RequestFailedException)
        {
            await CreateContainerAsync(cancellationToken);
            await blobClient.UploadAsync(BinaryData.FromString(content), cancellationToken);
        }
    }

    public async Task UploadFileAsync(string blobName, string pathToFile, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            try
            {
                await blobClient.UploadAsync(fs, cancellationToken);
            }
            catch (RequestFailedException)
            {
                await CreateContainerAsync(cancellationToken);
                await blobClient.UploadAsync(fs, cancellationToken);
            }
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

        try
        {
            await blobClient.UploadAsync(BinaryData.FromBytes(data), cancellationToken);
        }
        catch (RequestFailedException)
        {
            await CreateContainerAsync(cancellationToken);
            await blobClient.UploadAsync(BinaryData.FromBytes(data), cancellationToken);
        }
    }

    public async Task<string> UploadAsync(string content, CancellationToken cancellationToken = default)
    {
        string fileName = $"{Guid.NewGuid().ToString("N").ToLowerInvariant()}.txt";

        var blobClient = _blobContainerClient.GetBlobClient(fileName);

        try
        {
            await blobClient.UploadAsync(BinaryData.FromString(content), cancellationToken);
        }
        catch (RequestFailedException)
        {
            await CreateContainerAsync(cancellationToken);
            await blobClient.UploadAsync(BinaryData.FromString(content), cancellationToken);
        }

        return fileName;
    }

    public async Task<string> UploadAsync(Stream dataStream, CancellationToken cancellationToken = default)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        var blobClient = _blobContainerClient.GetBlobClient(fileName);

        try
        {
            await blobClient.UploadAsync(dataStream, cancellationToken);
        }
        catch (RequestFailedException)
        {
            await CreateContainerAsync(cancellationToken);
            await blobClient.UploadAsync(dataStream, cancellationToken);
        }

        return fileName;
    }

    #endregion

    #region Download

    public async Task<Stream?> DownloadAsStreamAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        try
        {
            var response = await blobClient.DownloadAsync(cancellationToken);
            return response.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
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
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        try
        {
            var localFile = new LocalFile();
            await blobClient.DownloadToAsync(localFile.FileStream, cancellationToken);

            return localFile;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
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

    #region LegalHold

    public async Task SetLegalHoldAsync(string blobName, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        await blobClient.SetLegalHoldAsync(hasLegalHold, cancellationToken);
    }

    public async Task<bool> HasLegalHoldAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

        return properties.Value?.HasLegalHold ?? false;
    }

    #endregion

    #endregion

    #region Sync

    #region CreateContainer

    public void CreateContainer()
    {
        if (_options.ShouldCreateIfNotExists)
        {
            _blobContainerClient.CreateIfNotExists(PublicAccessType.BlobContainer);
        }

        _blobContainerClient.SetAccessPolicy(_options.PublicAccessType);
    }

    #endregion

    #region Get

    public BlobMetadata? GetBlob(string blobName)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        if (!blobClient.Exists())
        {
            return null;
        }

        var properties = blobClient.GetProperties();

        return new BlobMetadata
        {
            Name = blobClient.Name,
            Uri = blobClient.Uri,
            Container = blobClient.BlobContainerName,
            Length = properties.Value.ContentLength
        };
    }

    public IEnumerable<BlobMetadata> GetBlobs(IEnumerable<string> blobNames)
    {
        foreach (var blob in blobNames)
        {
            var blobMetadata = GetBlob(blob);

            if (blobMetadata is not null)
            {
                yield return blobMetadata;
            }
        }
    }

    public IEnumerable<BlobMetadata> GetBlobList()
    {
        var blobs = _blobContainerClient.GetBlobs();

        foreach (var item in blobs.AsPages())
        {
            foreach (var blobItem in item.Values)
            {
                var blobMetadata = new BlobMetadata
                {
                    Name = blobItem.Name,
                    Length = blobItem.Properties.ContentLength ?? 0
                };

                yield return blobMetadata;
            }
        }
    }

    #endregion

    #region Upload

    public void UploadStream(string blobName, Stream dataStream)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        try
        {
            blobClient.Upload(dataStream);
        }
        catch (RequestFailedException)
        {
            CreateContainer();
            blobClient.UploadAsync(dataStream);
        }
    }

    public void Upload(string blobName, string content)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        try
        {
            blobClient.Upload(BinaryData.FromString(content));
        }
        catch (RequestFailedException)
        {
            CreateContainer();
            blobClient.Upload(BinaryData.FromString(content));
        }
    }

    public void UploadFile(string blobName, string pathToFile)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
        {
            try
            {
                blobClient.Upload(fs);
            }
            catch (RequestFailedException)
            {
                CreateContainer();
                blobClient.Upload(fs);
            }
        }
    }

    public void UploadStream(BlobMetadata blobMetadata, Stream dataStream)
    {
        UploadStream(blobMetadata.Name, dataStream);
    }

    public void UploadFile(BlobMetadata blobMetadata, string pathToFile)
    {
        UploadFile(blobMetadata.Name, pathToFile);
    }

    public void Upload(BlobMetadata blobMetadata, string content)
    {
        Upload(blobMetadata.Name, content);
    }

    public void Upload(BlobMetadata blobMetadata, byte[] data)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobMetadata.Name);

        try
        {
            blobClient.UploadAsync(BinaryData.FromBytes(data));
        }
        catch (RequestFailedException)
        {
            CreateContainer();
            blobClient.Upload(BinaryData.FromBytes(data));
        }
    }

    public string Upload(string content)
    {
        string fileName = $"{Guid.NewGuid().ToString("N").ToLowerInvariant()}.txt";

        var blobClient = _blobContainerClient.GetBlobClient(fileName);

        try
        {
            blobClient.Upload(BinaryData.FromString(content));
        }
        catch (RequestFailedException)
        {
            CreateContainer();
            blobClient.Upload(BinaryData.FromString(content));
        }

        return fileName;
    }

    public string Upload(Stream dataStream)
    {
        var fileName = Guid.NewGuid().ToString("N").ToLowerInvariant();
        var blobClient = _blobContainerClient.GetBlobClient(fileName);

        try
        {
            blobClient.Upload(dataStream);
        }
        catch (RequestFailedException)
        {
            CreateContainer();
            blobClient.Upload(dataStream);
        }

        return fileName;
    }

    #endregion

    #region Download

    public Stream? DownloadAsStream(string blobName)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        try
        {
            var response = blobClient.Download();
            return response.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
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
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        try
        {
            var localFile = new LocalFile();
            blobClient.DownloadTo(localFile.FileStream);

            return localFile;
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.BlobNotFound)
        {
            return null;
        }
    }

    public LocalFile? Download(BlobMetadata blobMetadata)
    {
        return Download(blobMetadata.Name);
    }

    #endregion

    #region Exists

    public bool Exists(string blobName)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        return blobClient.Exists();
    }

    public bool Exists(BlobMetadata blobMetadata)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobMetadata.Name);

        return blobClient.Exists();
    }

    public IEnumerable<bool> Exists(IEnumerable<string> blobNames)
    {
        foreach (var blob in blobNames)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob);
            yield return blobClient.Exists();
        }
    }

    public IEnumerable<bool> Exists(IEnumerable<BlobMetadata> blobs)
    {
        foreach (var blob in blobs)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob.Name);
            yield return blobClient.Exists();
        }
    }

    #endregion

    #region Delete

    public void Delete(string blobName)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        blobClient.Delete();
    }

    public void Delete(BlobMetadata blobMetadata)
    {
        Delete(blobMetadata.Name);
    }

    public void Delete(IEnumerable<string> blobNames)
    {
        foreach (var blobName in blobNames)
        {
            Delete(blobName);
        }
    }

    public void Delete(IEnumerable<BlobMetadata> blobsMetadata)
    {
        foreach (var blobMetadata in blobsMetadata)
        {
            Delete(blobMetadata.Name);
        }
    }

    #endregion

    #region LegalHold

    public void SetLegalHold(string blobName, bool hasLegalHold)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);

        blobClient.SetLegalHold(hasLegalHold);
    }

    public bool HasLegalHold(string blobName)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        var properties = blobClient.GetProperties();

        return properties.Value?.HasLegalHold ?? false;
    }

    #endregion

    #endregion
}