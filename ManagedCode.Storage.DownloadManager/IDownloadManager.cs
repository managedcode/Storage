using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.DownloadManager;

public interface IDownloadManager
{
    Task UploadStreamAsync(string fileName, Stream stream, CancellationToken cancellationToken = default);
    Task UploadIFormFileAsync(string fileName, IFormFile formFile, CancellationToken cancellationToken = default);
    Task UploadStreamAsync(BlobMetadata blobMetadata, Stream stream, CancellationToken cancellationToken = default);
    Task UploadIFormFileAsync(BlobMetadata blobMetadata, IFormFile formFile, CancellationToken cancellationToken = default);
    Task UploadAsync(BlobMetadata blobMetadata, string content, CancellationToken cancellationToken = default);
    Task UploadAsync(BlobMetadata blobMetadata, byte[] data, CancellationToken cancellationToken = default);
    Task UploadFileAsync(BlobMetadata blobMetadata, string pathToFile, CancellationToken cancellationToken = default);
    Task UploadFileAsync(string fileName, string pathToFile, CancellationToken cancellationToken = default);
}