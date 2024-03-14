using System.ComponentModel.DataAnnotations;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.SampleClient.Core;
using ManagedCode.Storage.SampleClient.Core.DTO.DeleteFile;
using ManagedCode.Storage.SampleClient.Core.DTO.DownloadFile;
using ManagedCode.Storage.SampleClient.Core.DTO.UploadFile;
using ManagedCode.Storage.SampleClient.Core.Services.Interfaces;

namespace ManagedCode.Storage.SampleClient.Domain.Services;

public class FileStorageService : IFileStorageService
{
    // Concrete storage will be provided depending on CurrentState.StorageProvider value
    private IStorage _storage;

    public FileStorageService(IStorage storage)
    {
        _storage = storage;
    }

    public async Task DeleteFileAsync(DeleteFileRequest request)
    {
        var existResult = await _storage.ExistsAsync(new ExistOptions
        {
            FileName = request.FileName
        });

        if (!existResult.Value)
        {
            throw new ValidationException($"File with name {request.FileName} not found.");
        }

        var result = await _storage.DeleteAsync(new DeleteOptions 
        {
            FileName = request.FileName
        });

        if (!result.IsSuccess)
        {
            throw new DeleteFileException(
                result.Errors != null && result.Errors.Any() ? 
                string.Join(' ', result.Errors.Select(e => e.Message)) : 
                "Unknown delete file exception without detailed errors");
        }
    }

    public async Task<DownloadFileResult> DownloadFileResult(DownloadFileRequest request)
    {
        var result = await _storage.DownloadAsync(new DownloadOptions 
        {
            FileName = request.FileName
        });
        if (result.IsSuccess)
        {
            return new DownloadFileResult 
            {
                FileStream = result.Value.FileStream,
                MimeType = result.Value.BlobMetadata?.MimeType
            };
        }
        else
        {
            throw new DownloadFileException(
                result.Errors != null && result.Errors.Any() ? 
                string.Join(' ', result.Errors.Select(e => e.Message)) : 
                "Unknown download file exception without detailed errors"
            );
        }
    }

    public async Task<UploadFileResult> UploadFileAsync(Stream fileStream, string? contentType = null, string fileName = "")
    {
        var result = await _storage.UploadAsync(fileStream, new UploadOptions
        {
            FileName = string.IsNullOrEmpty(fileName) ? Guid.NewGuid().ToString() : fileName,
            MimeType = contentType
        });

        if (result.IsSuccess)
        {
            return new UploadFileResult
            {
                FileName = result.Value.Name
            };
        }
        else 
        {
            throw new UploadFileException (
                result.Errors != null && result.Errors.Any() ? 
                string.Join(' ', result.Errors.Select(e => e.Message)) : 
                "Unknown upload file exception without detailed errors");
        }
    }
}
