using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Models;

namespace ManagedCode.Storage.Client;

public class StorageClient(HttpClient httpClient) : IStorageClient
{
    private long _chunkSize;

    public long ChunkSize
    {
        get
        {
            if (_chunkSize == 0)
                throw new InvalidOperationException("ChunkSize is not set");

            return _chunkSize;
        }
        set => _chunkSize = value;
    }

    public void SetChunkSize(long size)
    {
        ChunkSize = size;
    }

    public event EventHandler<ProgressStatus>? OnProgressStatusChanged;

    public async Task<Result<BlobMetadata>> UploadFile(Stream stream, string apiUrl, string contentName,
        CancellationToken cancellationToken = default)
    {
        using var streamContent = new StreamContent(stream);
        using var formData = new MultipartFormDataContent();
        formData.Add(streamContent, contentName, contentName);

        var response = await httpClient.PostAsync(apiUrl, formData, cancellationToken);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<BlobMetadata>.Fail(response.StatusCode, content);
    }

    public async Task<Result<BlobMetadata>> UploadFile(FileInfo fileInfo, string apiUrl, string contentName,
        CancellationToken cancellationToken = default)
    {
        using var streamContent = new StreamContent(fileInfo.OpenRead());

        using (var formData = new MultipartFormDataContent())
        {
            formData.Add(streamContent, contentName, contentName);

            var response = await httpClient.PostAsync(apiUrl, formData, cancellationToken);

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

                var response = await httpClient.PostAsync(apiUrl, formData, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: cancellationToken);
                    return result;
                }

                return Result<BlobMetadata>.Fail(response.StatusCode);
            }
        }
    }

    public async Task<Result<BlobMetadata>> UploadFile(string base64, string apiUrl, string contentName,
        CancellationToken cancellationToken = default)
    {
        var fileAsBytes = Convert.FromBase64String(base64);
        using var fileContent = new ByteArrayContent(fileAsBytes);

        using var formData = new MultipartFormDataContent();

        formData.Add(fileContent, contentName, contentName);

        var response = await httpClient.PostAsync(apiUrl, formData, cancellationToken);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Result<BlobMetadata>>(cancellationToken: cancellationToken);

        return Result<BlobMetadata>.Fail(response.StatusCode);
    }

    public async Task<Result<LocalFile>> DownloadFile(string fileName, string apiUrl, string? path = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetStreamAsync($"{apiUrl}/{fileName}", cancellationToken);
            var localFile = path is null
                ? await LocalFile.FromStreamAsync(response, fileName)
                : await LocalFile.FromStreamAsync(response, path, fileName);
            return Result<LocalFile>.Succeed(localFile);
        }
        catch (HttpRequestException e) when (e.StatusCode != null)
        {
            return Result<LocalFile>.Fail(e.StatusCode.Value);
        }
        catch (Exception)
        {
            return Result<LocalFile>.Fail(HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Result<uint>> UploadLargeFile(Stream file, string uploadApiUrl, string completeApiUrl, Action<double>? onProgressChanged,
        CancellationToken cancellationToken = default)
    {
        var bufferSize = ChunkSize;
        var buffer = new byte[bufferSize];
        var chunkIndex = 1;
        var partOfProgress = file.Length / bufferSize;
        var fileName = "file" + Guid.NewGuid();

        var semaphore = new SemaphoreSlim(0, 4);
        var tasks = new List<Task>();
        int bytesRead;
        while ((bytesRead = await file.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            var task = Task.Run(async () =>
            {
                using (var memoryStream = new MemoryStream(buffer, 0, bytesRead))
                {
                    var content = new StreamContent(memoryStream);
                    using (var formData = new MultipartFormDataContent())
                    {
                        formData.Add(content, "File", fileName);
                        formData.Add(new StringContent(chunkIndex.ToString()), "Payload.ChunkIndex");
                        formData.Add(new StringContent(bufferSize.ToString()), "Payload.ChunkSize");
                        await httpClient.PostAsync(uploadApiUrl, formData, cancellationToken);
                    }
                }

                semaphore.Release();
            }, cancellationToken);

            await semaphore.WaitAsync(cancellationToken);
            tasks.Add(task);
            onProgressChanged?.Invoke(partOfProgress * chunkIndex);
            chunkIndex++;
        }

        await Task.WhenAll(tasks.ToArray());

        var mergeResult = await httpClient.PostAsync(completeApiUrl, JsonContent.Create(fileName), cancellationToken);

        return await mergeResult.Content.ReadFromJsonAsync<Result<uint>>(cancellationToken: cancellationToken);
    }

    public async Task<Result<Stream>> GetFileStream(string fileName, string apiUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"{apiUrl}/{fileName}");
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                return Result<Stream>.Succeed(stream);
            }

            return Result<Stream>.Fail(response.StatusCode);
        }
        catch (HttpRequestException e) when (e.StatusCode != null)
        {
            return Result<Stream>.Fail(e.StatusCode.Value);
        }
        catch (Exception)
        {
            return Result<Stream>.Fail(HttpStatusCode.InternalServerError);
        }
    }
}