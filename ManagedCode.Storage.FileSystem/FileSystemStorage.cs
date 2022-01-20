using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem.Options;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.FileSystem
{
    public class FileSystemStorage : IBlobStorage
    {
        private string _path;

        public FileSystemStorage(StorageOptions storageOptions)
        {
            _path = Path.Combine(storageOptions.CommonPath, storageOptions.Path);
        }

        public void Dispose() { }

        #region Delete

        public async Task DeleteAsync(string blob, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            var filePath = Path.Combine(_path, blob);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public async Task DeleteAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            await DeleteAsync(blob.Name);
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
            var memoryStream = new MemoryStream();
            var filePath = Path.Combine(_path, blob);

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await fs.CopyToAsync(memoryStream, cancellationToken);
            }

            return memoryStream;
        }

        public async Task<Stream> DownloadAsStreamAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await DownloadAsStreamAsync(blob.Name, cancellationToken);
        }

        public async Task<LocalFile> DownloadAsync(string blob, CancellationToken cancellationToken = default)
        {
            var localFile = new LocalFile();
            var filePath = Path.Combine(_path, blob);

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await fs.CopyToAsync(localFile.FileStream, cancellationToken);
            }

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
            await Task.Yield();

            var filePath = Path.Combine(_path, blob);

            return File.Exists(filePath);
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

        public Task<Blob> GetBlobAsync(string blob, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<Blob> GetBlobListAsync(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public IAsyncEnumerable<Blob> GetBlobsAsync(IEnumerable<string> blobs, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region Upload

        public async Task UploadAsync(string blob, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }

            var filePath = Path.Combine(_path, blob);

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                dataStream.Seek(0, SeekOrigin.Begin);
                await dataStream.CopyToAsync(fs, cancellationToken);
            }
        }

        public async Task UploadAsync(string blob, string pathToFile = null, bool append = false, CancellationToken cancellationToken = default)
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
            await UploadAsync(blob.Name, pathToFile, append, cancellationToken);
        }

        #endregion
    }
}
