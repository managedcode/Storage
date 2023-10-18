using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Client;

public interface IStorageClient
{
    Task<Result<BlobMetadata>> UploadFile(Stream stream, string apiUrl, string contentName, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> UploadFile(FileInfo fileInfo, string apiUrl, string contentName, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> UploadFile(byte[] bytes, string apiUrl, string contentName, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> UploadFile(string base64, string apiUrl, string contentName, CancellationToken cancellationToken = default);
    Task<Result> UploadFileInChunks(Stream file, string apiUrl, int chunkSize, CancellationToken cancellationToken = default);
    Task<Result<LocalFile>> DownloadFile(string fileName, string apiUrl, string? path = null, CancellationToken cancellationToken = default);
}