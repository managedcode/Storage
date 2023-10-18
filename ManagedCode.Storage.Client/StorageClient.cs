using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Client;

public class StorageClient : IStorageClient
{
    private readonly HttpClient _httpClient;

    public StorageClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<Result<BlobMetadata>> UploadFile(Stream stream, string apiUrl, string contentName, CancellationToken cancellationToken = default)
    {
        var streamContent = new StreamContent(stream);

        using (var formData = new MultipartFormDataContent())
        {
            formData.Add(streamContent, contentName, contentName);

            var response = await _httpClient.PostAsync(apiUrl, formData, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: cancellationToken);
                return result;
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);

            return Result<BlobMetadata>.Fail(response.StatusCode, content);
        }
    }
    
    public async Task<Result<BlobMetadata>> UploadFile(FileInfo fileInfo, string apiUrl, string contentName, CancellationToken cancellationToken = default)
    {
        using var streamContent = new StreamContent(fileInfo.OpenRead());

        using (var formData = new MultipartFormDataContent())
        {
            formData.Add(streamContent, contentName, contentName);
            
            var response = await _httpClient.PostAsync(apiUrl, formData, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: cancellationToken);
                return result;
            }
            
            return Result<BlobMetadata>.Fail(response.StatusCode);
        }
    }
    
    public async Task<Result<BlobMetadata>> UploadFile(byte[] bytes, string apiUrl, string contentName, CancellationToken cancellationToken = default)
    {
        using (var stream = new MemoryStream())
        {
            stream.Write(bytes, 0, bytes.Length);

            using var streamContent = new StreamContent(stream);

            using (var formData = new MultipartFormDataContent())
            {
                formData.Add(streamContent, contentName, contentName);

                var response = await _httpClient.PostAsync(apiUrl, formData, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: cancellationToken);
                    return result;
                }
                
                return Result<BlobMetadata>.Fail(response.StatusCode);
            }
        }
    }   

    public async Task<Result<BlobMetadata>> UploadFile(string base64, string apiUrl, string contentName, CancellationToken cancellationToken = default)
    {
        byte[] fileAsBytes = Convert.FromBase64String(base64);
        using var fileContent = new ByteArrayContent(fileAsBytes);
        
        using var formData = new MultipartFormDataContent();
        
        formData.Add(fileContent, contentName, contentName);

        var response = await _httpClient.PostAsync(apiUrl, formData, cancellationToken);
            
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: cancellationToken);
        }
            
        return Result<BlobMetadata>.Fail(response.StatusCode);
    }
    
    public async Task<Result<LocalFile>> DownloadFile(string fileName, string apiUrl, string? path = null,  CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetStreamAsync($"{apiUrl}/{fileName}", cancellationToken);
            
            var localFile = path is null ? await LocalFile.FromStreamAsync(response, fileName) : await LocalFile.FromStreamAsync(response, path, fileName);

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (HttpRequestException e)
        {
            return Result<LocalFile>.Fail(e.StatusCode ?? HttpStatusCode.InternalServerError);
        }
    }
}