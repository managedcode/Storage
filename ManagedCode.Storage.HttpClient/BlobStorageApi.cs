using ManagedCode.Storage.Core;
using Refit;

namespace ManagedCode.Storage.HttpClient;

public interface IBlobStorageApi
{
    [Get("/api/blob-storage/download/{storageType}/{fileName}")]
    Task<IApiResponse<HttpContent>> DownloadFile(StorageType storageType, string fileName);

    [Delete("/api/blob-storage/delete/{storageType}/{fileName}")]
    Task<IApiResponse> DeleteFile(StorageType storageType, string fileName);

    [Multipart]
    [Post("/api/blob-storage/upload/{storageType}")]
    Task<IApiResponse<string>> UploadFile(
        StorageType storageType, 
        StreamPart file
        );
}