using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Azure
{
    public class AzureBlobStorage : IBlobStorage
    {
        private readonly BlobContainerClient _blobContainerClient;

        public AzureBlobStorage(AzureBlobStorageConnectionOptions connectionOptions)
        {
            _blobContainerClient = new BlobContainerClient(
                connectionOptions.ConnectionString,
                connectionOptions.Container
            );

            _blobContainerClient.CreateIfNotExists(PublicAccessType.BlobContainer);
        }

        public void Dispose()
        {
        }

        #region Delete

        public async Task DeleteAsync(string blob, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob);
            await blobClient.DeleteAsync(DeleteSnapshotsOption.None, null, cancellationToken);
        }

        public async Task DeleteAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob.Name);
            await blobClient.DeleteAsync(DeleteSnapshotsOption.None, null, cancellationToken);
        }

        public async Task DeleteAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            foreach (var blobName in blobs)
            {
                await DeleteAsync(blobName, cancellationToken);
            }
        }

        public async Task DeleteAsync(IEnumerable<Blob> blobs, CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                await DeleteAsync(blob, cancellationToken);
            }
        }

        #endregion

        public async Task<Stream> DownloadAsStreamAsync(string blob, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob);
            var res = await blobClient.DownloadStreamingAsync();

            return res.Value.Content;
        }

        public async Task<Stream> DownloadAsStreamAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await DownloadAsStreamAsync(blob.Name, cancellationToken);
        }

        public async Task<LocalFile> DownloadAsync(string blob, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob);
            var localFile = new LocalFile();

            await blobClient.DownloadToAsync(localFile.FileStream, cancellationToken);

            return localFile;
        }

        public async Task<LocalFile> DownloadAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await DownloadAsync(blob.Name, cancellationToken);
        }

        #region Exists

        public async Task<bool> ExistsAsync(string blob, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob);

            return await blobClient.ExistsAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob.Name);

            return await blobClient.ExistsAsync(cancellationToken);
        }

        public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobs, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach(var blob in blobs)
            {
                var blobClient = _blobContainerClient.GetBlobClient(blob);
                yield return await blobClient.ExistsAsync(cancellationToken);
            }
        }

        public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<Blob> blobs,
             [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                var blobClient = _blobContainerClient.GetBlobClient(blob.Name);
                yield return await blobClient.ExistsAsync(cancellationToken);
            }
        }

        #endregion

        public async Task<Blob> GetBlobAsync(string blob, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            var blobClient = _blobContainerClient.GetBlobClient(blob);
           
            return new Blob()
            {
                Name = blobClient.Name,
                Uri = blobClient.Uri
            };
        }

        public IAsyncEnumerable<Blob> GetBlobsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<Blob> GetBlobListAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #region Upload

        public async Task UploadAsync(string blob, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob);
            await blobClient.UploadAsync(dataStream, cancellationToken);
        }

        public async Task UploadAsync(string blob, string pathToFile, bool append = false, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blob);

            using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
            {
                await blobClient.UploadAsync(fs, cancellationToken);
            }
        }

        public async Task UploadAsync(Blob blob, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
        {
            await UploadAsync(blob.Name, dataStream, append, cancellationToken);
        }

        public async Task UploadAsync(Blob blob, string pathToFile, bool append = false, CancellationToken cancellationToken = default)
        {
            await UploadAsync(blob.Name, pathToFile, append, cancellationToken);
        }

        #endregion
    }
}
