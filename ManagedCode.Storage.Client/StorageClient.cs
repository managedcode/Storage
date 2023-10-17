using System;
using System.IO;
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
    
    public async Task<string> DownloadFile(string fileName, string apiUrl, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetStreamAsync($"{apiUrl}/{fileName}", cancellationToken);
        
        Stream responseStream = response;

        using (MemoryStream memoryStream = new MemoryStream())
        {
            byte[] buffer = new byte[8192];

            int bytesRead;
            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);
            }

            byte[] byteArray = memoryStream.ToArray();

            string content = System.Text.Encoding.UTF8.GetString(byteArray);
            
            return content;
        }
    }
}