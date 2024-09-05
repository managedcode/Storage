using ManagedCode.Communication;
using ManagedCode.Storage.BlobClient.Interfaces;
using ManagedCode.Storage.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;


namespace ManagedCode.Storage.BlobClient.Services;

public class StorageClientService : IStorageClientService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public StorageClientService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<Result<BlobMetadata>> UploadFileAsync(LocalFile file, CancellationToken cancellationToken = default)
    {
        var uploadUrl = _configuration["ApiRoutes:BaseUrl"] + _configuration["ApiRoutes:Upload"];
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.FileStream), "formFile", file.Name);

        var response = await _httpClient.PostAsync(uploadUrl, content, cancellationToken);
        return await HandleResponseAsync<BlobMetadata>(response, cancellationToken);
    }

    public async Task<Result<LocalFile>> DownloadFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var downloadUrl = $"{_configuration["ApiRoutes:BaseUrl"]}{_configuration["ApiRoutes:Download"]}/{fileName}";
        var response = await _httpClient.GetAsync(downloadUrl, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var localFile = await LocalFile.FromStreamAsync(stream, fileName);
            return Result<LocalFile>.Succeed(localFile);
        }

        return Result<LocalFile>.Fail(response.StatusCode);
    }

    public async Task<Result<bool>> DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var deleteUrl = $"{_configuration["ApiRoutes:BaseUrl"]}{_configuration["ApiRoutes:Delete"]}/{fileName}";
        var response = await _httpClient.DeleteAsync(deleteUrl, cancellationToken);

        return response.IsSuccessStatusCode
            ? Result<bool>.Succeed(true)
            : Result<bool>.Fail(response.StatusCode);
    }

    private async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Result<T>>(cancellationToken);

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<T>.Fail(response.StatusCode, errorContent);
    }
}
