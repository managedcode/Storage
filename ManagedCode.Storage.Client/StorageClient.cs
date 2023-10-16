using System;
using System.IO;
using System.Net.Http;
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
    
    public async Task<BlobMetadata> UploadFile(Stream stream, string apiUrl, CancellationToken cancellationToken = default)
    {
        var streamContent = new StreamContent(stream);

        using (var formData = new MultipartFormDataContent())
        {
            formData.Add(streamContent, $"file_{Guid.NewGuid().ToString()}");

            var response = await _httpClient.PostAsync(apiUrl, formData);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStreamAsync(cancellationToken);
                var result = await JsonSerializer.DeserializeAsync<BlobMetadata>(content, cancellationToken: cancellationToken);
                return result;
            }
            else
            {
                return null;
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