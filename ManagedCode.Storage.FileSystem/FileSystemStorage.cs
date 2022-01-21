using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.FileSystem
{
    public class FileSystemStorage : IBlobStorage
    {
        private string _path;

        public FileSystemStorage(StorageOptions storageOptions)
        {
            _path = Path.Combine(storageOptions.CommonPath, storageOptions.Path);
            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
        }
        
        public void Dispose() { }

        #region Delete

        public async Task DeleteAsync(string blob, CancellationToken cancellationToken = default)
        {
            EnsureDirectoryExists();
            
            await Task.Yield();

            var filePath = Path.Combine(_path, blob);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public async Task DeleteAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            EnsureDirectoryExists();
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
            EnsureDirectoryExists();
            var memoryStream = new MemoryStream();
            var filePath = Path.Combine(_path, blob);

            if (File.Exists(filePath))
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await fs.CopyToAsync(memoryStream, cancellationToken);
                }

                return memoryStream;
            }

            return memoryStream;
        }

        public async Task<Stream> DownloadAsStreamAsync(Blob blob, CancellationToken cancellationToken = default)
        {
            return await DownloadAsStreamAsync(blob.Name, cancellationToken);
        }

        public async Task<LocalFile> DownloadAsync(string blob, CancellationToken cancellationToken = default)
        {
            EnsureDirectoryExists();
            
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
            EnsureDirectoryExists();
            
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
            EnsureDirectoryExists();
            
            var fileInfo = new FileInfo(Path.Combine(_path, blob));

            if (fileInfo.Exists)
            {
                var result = new Blob
                {
                    Name = fileInfo.Name,
                    Uri = new Uri(Path.Combine(_path, blob))
                };
                return Task.FromResult(result);
            }

            return null;
        }

        public async IAsyncEnumerable<Blob> GetBlobListAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var file in Directory.EnumerateFiles(_path))
            {
                yield return await GetBlobAsync(file, cancellationToken);
            }
        }

        public async IAsyncEnumerable<Blob> GetBlobsAsync(IEnumerable<string> blobs,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var file in blobs)
            {
                yield return await GetBlobAsync(file, cancellationToken);
            }
        }

        #endregion

        #region Upload
        
        public async Task UploadStreamAsync(string blob, Stream dataStream, CancellationToken cancellationToken = default)
        {
            EnsureDirectoryExists();
            var filePath = Path.Combine(_path, blob);

            using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                dataStream.Seek(0, SeekOrigin.Begin);
                await dataStream.CopyToAsync(fs, cancellationToken);
            }
        }

        public async Task UploadFileAsync(string blob, string pathToFile = null, CancellationToken cancellationToken = default)
        {
            using (var fs = new FileStream(pathToFile, FileMode.Open, FileAccess.Read))
            {
                await UploadStreamAsync(blob, fs, cancellationToken);
            }
        }

        public async Task UploadAsync(string blob, string content, CancellationToken cancellationToken = default)
        {
            EnsureDirectoryExists();
            var filePath = Path.Combine(_path, blob);
            await File.WriteAllTextAsync(filePath, content, cancellationToken);
        }
        
        public Task UploadStreamAsync(Blob blob, Stream dataStream, CancellationToken cancellationToken = default)
        {
            return UploadStreamAsync(blob.Name, dataStream, cancellationToken);
        }

        public Task UploadFileAsync(Blob blob, string pathToFile, CancellationToken cancellationToken = default)
        {
            return UploadFileAsync(blob.Name, pathToFile, cancellationToken);
        }

        public async Task UploadAsync(Blob blob, string content, CancellationToken cancellationToken = default)
        {
            await UploadAsync(blob.Name, content, cancellationToken);
        }

        public async Task UploadAsync(Blob blob, byte[] data, CancellationToken cancellationToken = default)
        {
            EnsureDirectoryExists();
            var filePath = Path.Combine(_path, blob.Name);

            await File.WriteAllBytesAsync(filePath, data, cancellationToken);
        }
        #endregion
    }
}
