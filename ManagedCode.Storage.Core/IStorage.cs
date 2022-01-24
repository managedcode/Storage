using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core
{
    public interface IStorage : IDisposable
    {
        IAsyncEnumerable<Blob> GetBlobListAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<Blob> GetBlobsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default);
        Task<Blob> GetBlobAsync(string blob, CancellationToken cancellationToken = default);

        Task UploadStreamAsync(string blob, Stream dataStream, CancellationToken cancellationToken = default);
        Task UploadFileAsync(string blob, string pathToFile, CancellationToken cancellationToken = default);
        Task UploadAsync(string blob, string content, CancellationToken cancellationToken = default);
        Task UploadStreamAsync(Blob blob, Stream dataStream, CancellationToken cancellationToken = default);
        Task UploadFileAsync(Blob blob, string pathToFile, CancellationToken cancellationToken = default);
        Task UploadAsync(Blob blob, string content, CancellationToken cancellationToken = default);
        Task UploadAsync(Blob blob, byte[] data, CancellationToken cancellationToken = default);

        Task<Stream> DownloadAsStreamAsync(string blob, CancellationToken cancellationToken = default);
        Task<Stream> DownloadAsStreamAsync(Blob blob, CancellationToken cancellationToken = default);
        Task<LocalFile> DownloadAsync(string blob, CancellationToken cancellationToken = default);
        Task<LocalFile> DownloadAsync(Blob blob, CancellationToken cancellationToken = default);

        Task DeleteAsync(string blob, CancellationToken cancellationToken = default);
        Task DeleteAsync(Blob blob, CancellationToken cancellationToken = default);
        Task DeleteAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default);
        Task DeleteAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(string blob, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Blob blob, CancellationToken cancellationToken = default);
        IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default);
        IAsyncEnumerable<bool> ExistsAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default);
    }
}