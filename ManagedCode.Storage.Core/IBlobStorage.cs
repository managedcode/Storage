using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Core
{
    public interface IBlobStorage : IDisposable
    {
        IAsyncEnumerable<Blob> GetBlobListAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<Blob> GetBlobsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default);
        Task<Blob> GetBlobAsync(string blob, CancellationToken cancellationToken = default);

        Task UploadAsync(string blob, Stream dataStream, bool append = false, CancellationToken cancellationToken = default);
        Task UploadAsync(string blob, string pathToFile, bool append = false, CancellationToken cancellationToken = default);
        Task UploadAsync(Blob blob, Stream dataStream, bool append = false, CancellationToken cancellationToken = default);
        Task UploadAsync(Blob blob, string pathToFile, bool append = false, CancellationToken cancellationToken = default);

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