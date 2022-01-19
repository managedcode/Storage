using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Azure
{
    public class AzureBlobStorage : IAzureBlobStorage
    {
        public AzureBlobStorage(/*AzureBlobStorageConnectionOptions connectionOptions*/)
        {
        //    var blobServiceClient = new BlobServiceClient(connectionOptions.ConnectionString);
        }

        public Task DeleteAsync(string blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public Task<Stream> DownloadAsStreamAsync(string blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<Stream> DownloadAsStreamAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<LocalFile> DownloadAsync(string blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<LocalFile> DownloadAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> ExistsAsync(string blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> ExistsAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<bool> ExistsAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<Blob> GetBlob(string blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<Blob> GetBlob(Blob blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<Blob> GetBlob(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<Blob> GetBlob(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<Blob> GetBlobListAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task UploadAsync(string blob, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task UploadAsync(string blob, string pathToFile, bool append = false, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task UploadAsync(Blob blob, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public Task UploadAsync(Blob blob, string pathToFile, bool append = false, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
