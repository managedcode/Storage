using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Client;

public interface IStorageClient : IUploader, IDownloader
{
    void SetChunkSize(long size);
    Task<Result<BlobMetadata>> UploadFile(Stream stream, string apiUrl, string contentName, CancellationToken cancellationToken = default);
    Task<Result<LocalFile>> DownloadFile(string fileName, string apiUrl, string? path = null, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> UploadFile(string base64, string apiUrl, string contentName, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> UploadFile(byte[] bytes, string apiUrl, string contentName, CancellationToken cancellationToken = default);
    Task<Result<BlobMetadata>> UploadFile(FileInfo fileInfo, string apiUrl, string contentName, CancellationToken cancellationToken = default);
    Task<Result<uint>> UploadLargeFile(Stream file,
        string uploadApiUrl,
        string completeApiUrl,
        Action<double>? onProgressChanged,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Event triggered when the progress status changes during an upload or download operation.
    /// </summary>
    /// <remarks>
    ///     The event handler receives an argument of type <see cref="ProgressStatus"/> which contains detailed information about the progress of the operation.
    ///     This includes the file name, progress percentage, total bytes, transferred bytes, elapsed time, remaining time, speed, and any error message.
    /// </remarks>
    event EventHandler<ProgressStatus> OnProgressStatusChanged;
}


public record ProgressStatus(
    string File, 
    float Progress,
    long TotalBytes, 
    long TransferredBytes, 
    TimeSpan Elapsed, 
    TimeSpan Remaining, 
    string Speed, 
    string? Error = null);