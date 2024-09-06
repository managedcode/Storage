using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.HttpClient;

public interface IBlobStorageClient
{
    Task<Result<LocalFile>> DownloadFile(StorageType storageType, string fileName);
    Task<Result<bool>> DeleteFile(StorageType storageType, string fileName);
    Task<Result<BlobMetadata>> UploadFile(StorageType storageType, FileStream file);
}