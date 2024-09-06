using System.Text.Json;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Refit;

namespace ManagedCode.Storage.HttpClient;

public class BlobStorageClient(IBlobStorageApi api) : IBlobStorageClient
{
    public async Task<Result<LocalFile>> DownloadFile(StorageType storageType, string fileName)
    {
        var response = await api.DownloadFile(storageType, fileName).ConfigureAwait(false);

        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var localFile = await LocalFile.FromStreamAsync(stream, fileName).ConfigureAwait(false);
            return Result<LocalFile>.Succeed(localFile);
        }

        return Result<LocalFile>.Fail(response.StatusCode);
    }

    public async Task<Result<bool>> DeleteFile(StorageType storageType, string fileName)
    {
        var response = await api.DeleteFile(storageType, fileName).ConfigureAwait(false);
        return response.IsSuccessStatusCode ? Result<bool>.Succeed(true) : Result<bool>.Fail(response.StatusCode);
    }

    public async Task<Result<BlobMetadata>> UploadFile(StorageType storageType, FileStream file)
    {
        var response = await api.UploadFile(storageType, new StreamPart(file, file.Name, "application/octet-stream")).ConfigureAwait(false);
        if (response is { IsSuccessStatusCode: true, Content: not null })
        {
            var result = JsonSerializer.Deserialize<BlobMetadata>(response.Content);
            if (result is not null)
            {
                return Result<BlobMetadata>.Succeed(result);
            }
        }

        return Result<BlobMetadata>.Fail(response.StatusCode, response.Error?.Content ?? "Error occured on file uploading");
    }
}