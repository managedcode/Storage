using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.BlobClient.Interfaces;

public interface IStorageClientService
{
    Task<Result<BlobMetadata>> UploadFileAsync(LocalFile formFile, CancellationToken cancellationToken);
    Task<Result<LocalFile>> DownloadFileAsync(string fileName, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteFileAsync(string fileName, CancellationToken cancellationToken);
}
