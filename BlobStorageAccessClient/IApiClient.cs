using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace BlobStorageAccessClient;

public interface IApiClient
{
    Task<Result<BlobMetadata>> UploadFileAsync(LocalFile formFile, CancellationToken token);

    Task<Result<LocalFile>> DownloadFileAsync(string fileName, CancellationToken cancellationToken);

    Task<Result<bool>> DeleteFileAsync(string fileName, CancellationToken token);
}