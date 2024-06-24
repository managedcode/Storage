using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Client;

public interface IStorageClient
{
    void SetChunkSize(long size);

    event EventHandler<ProgressStatus> OnProgressStatusChanged;

    Task<Result<BlobMetadata>> UploadFile(string base64, string apiUrl, string contentName, CancellationToken cancellationToken = default);

    Task<Result<BlobMetadata>> UploadFile(byte[] bytes, string apiUrl, string contentName, CancellationToken cancellationToken = default);

    Task<Result<BlobMetadata>> UploadFile(FileInfo fileInfo, string apiUrl, string contentName, CancellationToken cancellationToken = default);

    Task<Result<BlobMetadata>> UploadFile(Stream stream, string apiUrl, string contentName, CancellationToken cancellationToken = default);

    Task<Result<LocalFile>> DownloadFile(string fileName, string apiUrl, string? path = null, CancellationToken cancellationToken = default);

    Task<Result<uint>> UploadLargeFile(Stream file, string uploadApiUrl, string completeApiUrl, Action<double>? onProgressChanged,
        CancellationToken cancellationToken = default);

    Task<Result<Stream>> GetFileStream(string fileName, string apiUrl, CancellationToken cancellationToken = default);
}