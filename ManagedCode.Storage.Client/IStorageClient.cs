using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Client;

public interface IStorageClient
{
    Task<Result<BlobMetadata>> UploadFile(Stream stream, string apiUrl, string contentName, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> UploadFile(FileInfo fileInfo, string apiUrl,
        CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> UploadFile(byte[] bytes, string apiUrl, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> UploadFile(string base64, string apiUrl, CancellationToken cancellationToken = default);
    Task<string> DownloadFile(string fileName, string apiUrl, CancellationToken cancellationToken = default);
}