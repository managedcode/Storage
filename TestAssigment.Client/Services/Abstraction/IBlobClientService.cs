using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace TestAssigmentClient.Services.Abstraction
{
    public interface IBlobClientService
    {
        Task<Result<BlobMetadata>> UploadFileAsync(LocalFile file, CancellationToken cancellationToken = default);
        Task<Result<LocalFile>> DownloadFileAsync(string fileName, CancellationToken cancellationToken = default);


    }
}
