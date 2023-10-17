using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Communication.Extensions;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Client;

public class StorageClient
{
    private readonly HttpClient _httpClient;

    public StorageClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<Result<BlobMetadata>> UploadFile(Stream stream, string apiUrl, string contentName, string fileName, CancellationToken cancellationToken = default)
    {
        var streamContent = new StreamContent(stream);
        
        using (var formData = new MultipartFormDataContent { {streamContent, contentName, fileName} })
        {
            var response = await _httpClient.PostAsync(apiUrl, formData, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: cancellationToken);
                return result;
            }
            else
            {
                return Result.Fail();
            }
        }
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