using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core;

public interface IStorage : IDisposable
{
    Task CreateContainerAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<BlobMetadata> GetBlobListAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<BlobMetadata> GetBlobsAsync(IEnumerable<string> blobNames, CancellationToken cancellationToken = default);
    Task<BlobMetadata?> GetBlobAsync(string blobName, CancellationToken cancellationToken = default);

    Task UploadStreamAsync(string blobName, Stream dataStream, CancellationToken cancellationToken = default);
    Task UploadFileAsync(string blobName, string pathToFile, CancellationToken cancellationToken = default);
    Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default);
    Task UploadStreamAsync(BlobMetadata blobMetadata, Stream dataStream, CancellationToken cancellationToken = default);
    Task UploadFileAsync(BlobMetadata blobMetadata, string pathToFile, CancellationToken cancellationToken = default);
    Task UploadAsync(BlobMetadata blobMetadata, string content, CancellationToken cancellationToken = default);
    Task UploadAsync(BlobMetadata blobMetadata, byte[] data, CancellationToken cancellationToken = default);
    Task<string> UploadAsync(string content, CancellationToken cancellationToken = default);
    Task<string> UploadAsync(Stream dataStream, CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsStreamAsync(string blobName, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsStreamAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default);
    Task<LocalFile?> DownloadAsync(string blobName, CancellationToken cancellationToken = default);
    Task<LocalFile?> DownloadAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);
    Task DeleteAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default);
    Task DeleteAsync(IEnumerable<string> blobNames, CancellationToken cancellationToken = default);
    Task DeleteAsync(IEnumerable<BlobMetadata> blobNames, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default);
    IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobNames, CancellationToken cancellationToken = default);
    IAsyncEnumerable<bool> ExistsAsync(IEnumerable<BlobMetadata> blobs, CancellationToken cancellationToken = default);

    Task SetLegalHoldAsync(string blobName, bool hasLegalHold, CancellationToken cancellationToken = default);
    Task<bool> HasLegalHoldAsync(string blobName, CancellationToken cancellationToken = default);


    void CreateContainer();

    IEnumerable<BlobMetadata> GetBlobList();
    IEnumerable<BlobMetadata> GetBlobs(IEnumerable<string> blobNames);
    BlobMetadata? GetBlob(string blobName);

    void UploadStream(string blobName, Stream dataStream);
    void UploadFile(string blobName, string pathToFile);
    void Upload(string blobName, string content);
    void UploadStream(BlobMetadata blobMetadata, Stream dataStream);
    void UploadFile(BlobMetadata blobMetadata, string pathToFile);
    void Upload(BlobMetadata blobMetadata, string content);
    void Upload(BlobMetadata blobMetadata, byte[] data);
    string Upload(string content);
    string Upload(Stream dataStream);

    Stream? DownloadAsStream(string blobName);
    Stream? DownloadAsStream(BlobMetadata blobMetadata);
    LocalFile? Download(string blobName);
    LocalFile? Download(BlobMetadata blobMetadata);

    void Delete(string blobName);
    void Delete(BlobMetadata blobMetadata);
    void Delete(IEnumerable<string> blobNames);
    void Delete(IEnumerable<BlobMetadata> blobMetadatas);

    bool Exists(string blobName);
    bool Exists(BlobMetadata blobMetadata);
    IEnumerable<bool> Exists(IEnumerable<string> blobNames);
    IEnumerable<bool> Exists(IEnumerable<BlobMetadata> blobs);

    void SetLegalHold(string blobName, bool hasLegalHold);
    bool HasLegalHold(string blobName);
}