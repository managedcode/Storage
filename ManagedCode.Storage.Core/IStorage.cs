using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core;

public interface IStorage : IDisposable
{
    IAsyncEnumerable<BlobMetadata> GetBlobListAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<BlobMetadata> GetBlobsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default);
    Task<BlobMetadata> GetBlobAsync(string blob, CancellationToken cancellationToken = default);

    Task UploadStreamAsync(string blob, Stream dataStream, CancellationToken cancellationToken = default);
    Task UploadFileAsync(string blob, string pathToFile, CancellationToken cancellationToken = default);
    Task UploadAsync(string blob, string content, CancellationToken cancellationToken = default);
    Task UploadStreamAsync(BlobMetadata blobMetadata, Stream dataStream, CancellationToken cancellationToken = default);
    Task UploadFileAsync(BlobMetadata blobMetadata, string pathToFile, CancellationToken cancellationToken = default);
    Task UploadAsync(BlobMetadata blobMetadata, string content, CancellationToken cancellationToken = default);
    Task UploadAsync(BlobMetadata blobMetadata, byte[] data, CancellationToken cancellationToken = default);
    Task UploadAsync(string content, CancellationToken cancellationToken = default);
    Task UploadAsync(Stream dataStream, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsStreamAsync(string blob, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsStreamAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default);
    Task<LocalFile> DownloadAsync(string blob, CancellationToken cancellationToken = default);
    Task<LocalFile> DownloadAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default);

    Task DeleteAsync(string blob, CancellationToken cancellationToken = default);
    Task DeleteAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default);
    Task DeleteAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default);
    Task DeleteAsync(IEnumerable<BlobMetadata> blobs, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string blob, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(BlobMetadata blobMetadata, CancellationToken cancellationToken = default);
    IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default);
    IAsyncEnumerable<bool> ExistsAsync(IEnumerable<BlobMetadata> blobs, CancellationToken cancellationToken = default);
}