
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Helpers;
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
    
    public async Task<Result> UploadLargeFileUsingStream(Stream file, 
        string сreateApiUrl,  
        string uploadApiUrl, 
        string completeApiUrl, 
        Action<double>? onProgressChanged,
        CancellationToken cancellationToken)
    {
        var bufferSize = 4096000; //TODO: chunk size get from config
        var buffer = new byte[bufferSize];
        int bytesRead;
        int chunkIndex = 1;
        var fileCRC = Crc32Helper.Calculate(file);
        var partOfProgress = file.Length / bufferSize;
        
        var createdFileResponse = await _httpClient.PostAsync(сreateApiUrl, JsonContent.Create(file.Length), cancellationToken);
        var createdFile = await createdFileResponse.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: cancellationToken);
        
        var semaphore = new SemaphoreSlim(0, 4);
        var tasks = new List<Task>();
        while ((bytesRead = await file.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    semaphore.WaitAsync(cancellationToken);
                    using (var memoryStream = new MemoryStream(buffer, 0, bytesRead))
                    {
                        var content = new StreamContent(memoryStream);

                        using (var chunk = new MultipartFormDataContent())
                        {
                            chunk.Add(content, "chunk", createdFile.Value.FullName);
                            chunk.Add(new StringContent(createdFile.Value.FullName), "Payload.BlobName");
                            chunk.Add(new StringContent(chunkIndex.ToString()), "Payload.ChunkIndex");
                            chunk.Add(new StringContent(bufferSize.ToString()), "Payload.ChunkSize");
                            chunk.Add(new StringContent(fileCRC.ToString()), "Payload.FullCRC");

                            _httpClient.PostAsync(uploadApiUrl, chunk, cancellationToken);
                        }
                        
                        onProgressChanged?.Invoke(partOfProgress * chunkIndex);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);
            
            tasks.Add(task);
            
            chunkIndex++;
        }
        
        await Task.WhenAll(tasks.ToArray());
        
        var mergeResult = await _httpClient.PostAsync(completeApiUrl, JsonContent.Create(
            new {fileCrc = fileCRC, blobName = createdFile.Value.FullName}), cancellationToken);
        
        return await mergeResult.Content.ReadFromJsonAsync<Result>(cancellationToken: cancellationToken);
    }
    
    public async Task<Result> UploadLargeFileUsingMerge(Stream file,
        string uploadApiUrl, 
        string mergeApiUrl, 
        Action<double>? onProgressChanged,
        CancellationToken cancellationToken)
    {
        var bufferSize = 4096000; //TODO: chunk size get from config
        var buffer = new byte[bufferSize];
        int bytesRead;
        int chunkIndex = 1;
        var fileCRC = Crc32Helper.Calculate(file);
        var partOfProgress = file.Length / bufferSize;
        
        var semaphore = new SemaphoreSlim(0, 4);
        var tasks = new List<Task<HttpResponseMessage>>();
        while ((bytesRead = await file.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            var task = Task.Run(() =>
            {
                try
                {
                    semaphore.WaitAsync(cancellationToken);
                    using (var memoryStream = new MemoryStream(buffer, 0, bytesRead))
                    {
                        var content = new StreamContent(memoryStream);

                        using (var chunk = new MultipartFormDataContent())
                        {
                            chunk.Add(content, "chunk");
                            chunk.Add(new StringContent(chunkIndex.ToString()), "Payload.ChunkIndex");
                            chunk.Add(new StringContent(bufferSize.ToString()), "Payload.ChunkSize");
                            chunk.Add(new StringContent(fileCRC.ToString()), "Payload.FullCRC");

                            var result = _httpClient.PostAsync(uploadApiUrl, chunk, cancellationToken);
                            onProgressChanged?.Invoke(partOfProgress * chunkIndex);
                            
                            return result;
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);
            
            tasks.Add(task);
            chunkIndex++;
        }
        
        var tasksResult =  await Task.WhenAll(tasks.ToArray());
        var blobNames = tasksResult
            .Select(async x =>
            {
                var content = await x.Content.ReadFromJsonAsync<Result<string>>(cancellationToken: cancellationToken);
                return content.Value;
            });
        
        var mergeResult = await _httpClient.PostAsync(mergeApiUrl, JsonContent.Create(
            new {fileCrc = fileCRC, blobNames = blobNames}), cancellationToken);
        
        return await mergeResult.Content.ReadFromJsonAsync<Result>(cancellationToken: cancellationToken);
    }
}