using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Gcp
{
    public class GoogleStorage : IBlobStorage
    {
        private readonly string _bucket;
        private readonly StorageClient _storageClient;

        public GoogleStorage(GoogleCredential googleCredential, string bucket, string projectId)
        {
            _bucket = bucket;

            _storageClient = StorageClient.Create(googleCredential);

            try
            {
                _storageClient.CreateBucket(projectId, bucket);
            }
            catch 
            { }
        }

        public void Dispose() { }

        #region Delete

        public async Task DeleteAsync(string blob, CancellationToken cancellationToken = default)
        {
            await _storageClient.DeleteObjectAsync(_bucket, blob, null, cancellationToken);
        }

        public async Task DeleteAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            await DeleteAsync(blob.Name, cancellationToken);
        }

        public async Task DeleteAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                await DeleteAsync(blob, cancellationToken);
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

        #region Download

        public async Task<Stream> DownloadAsStreamAsync(string blob, CancellationToken cancellationToken = default)
        {
            var stream = new MemoryStream();
            await _storageClient.DownloadObjectAsync(_bucket, blob, stream, null, cancellationToken);

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public async Task<Stream> DownloadAsStreamAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await DownloadAsStreamAsync(blob.Name, cancellationToken);
        }

        public async Task<LocalFile> DownloadAsync(string blob, CancellationToken cancellationToken = default)
        {
            var localFile = new LocalFile();

            await _storageClient.DownloadObjectAsync(_bucket, blob, 
                localFile.FileStream, null, cancellationToken);
            
            return localFile;
        }

        public async Task<LocalFile> DownloadAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await DownloadAsync(blob.Name, cancellationToken);
        }

        #endregion

        #region Exists

        public async Task<bool> ExistsAsync(string blob, CancellationToken cancellationToken = default)
        {
            try
            {
                await _storageClient.GetObjectAsync(_bucket, blob, null, cancellationToken);

                return true;
            }
            catch (GoogleApiException)
            {
                return false;
            }
        }

        public async Task<bool> ExistsAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await ExistsAsync(blob.Name, cancellationToken);
        }

        public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<string> blobs,
             [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                yield return await ExistsAsync(blob, cancellationToken);
            }
        }

        public async IAsyncEnumerable<bool> ExistsAsync(IEnumerable<Blob> blobs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                yield return await ExistsAsync(blob, cancellationToken);
            }
        }

        #endregion

        #region Get

        public async Task<Blob> GetBlobAsync(string blob, CancellationToken cancellationToken = default)
        {
            var obj = await _storageClient.GetObjectAsync(_bucket, blob, null, cancellationToken);

            return new Blob
            {
                Name = obj.Name,
                Uri = new System.Uri(obj.MediaLink)
            };
        }

        public IAsyncEnumerable<Blob> GetBlobListAsync(CancellationToken cancellationToken = default)
        {
            return _storageClient.ListObjectsAsync(_bucket, string.Empty, 
                new ListObjectsOptions { Projection = Projection.Full })
                .Select(
                    x => new Blob
                    {
                        Name = x.Name,
                        Uri = new System.Uri(x.MediaLink)
                    }
                );
        }

        public async IAsyncEnumerable<Blob> GetBlobsAsync(IEnumerable<string> blobs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var blob in blobs)
            {
                yield return await GetBlobAsync(blob, cancellationToken);
            }
        }

        #endregion

        #region Upload

        public async Task UploadAsync(string blob, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
        {
            await _storageClient.UploadObjectAsync(_bucket, blob, null, dataStream, null, cancellationToken);
        }

        public async Task UploadAsync(string blob, string pathToFile, bool append = false, CancellationToken cancellationToken = default)
        {
            using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
            {
                await UploadAsync(blob, fs, append, cancellationToken);
            }
        }

        public async Task UploadAsync(Blob blob, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
        {
            await UploadAsync(blob.Name, dataStream, append, cancellationToken);
        }

        public async Task UploadAsync(Blob blob, string pathToFile, bool append = false, CancellationToken cancellationToken = default)
        {
            using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
            {
                await UploadAsync(blob, fs, append, cancellationToken);
            }
        }

        #endregion
    }
}
