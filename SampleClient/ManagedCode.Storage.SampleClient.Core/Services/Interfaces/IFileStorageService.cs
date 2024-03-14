using ManagedCode.Storage.SampleClient.Core.DTO.DeleteFile;
using ManagedCode.Storage.SampleClient.Core.DTO.DownloadFile;
using ManagedCode.Storage.SampleClient.Core.DTO.UploadFile;

namespace ManagedCode.Storage.SampleClient.Core.Services.Interfaces;

public interface IFileStorageService
{
    Task<UploadFileResult> UploadFileAsync(Stream fileStream, string? contentType = null, string fileName = "");
    Task<DownloadFileResult> DownloadFileResult(DownloadFileRequest request);
    Task DeleteFileAsync(DeleteFileRequest request);
}