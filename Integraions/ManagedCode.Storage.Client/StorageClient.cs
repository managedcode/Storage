using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.MimeTypes;
using System.Text.Json;

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
        if (ChunkSize <= 0)
        {
            throw new InvalidOperationException("Chunk size must be configured via SetChunkSize before uploading large files.");
        }

        var uploadId = Guid.NewGuid().ToString("N");
        var resolvedFileName = file is FileStream fs ? Path.GetFileName(fs.Name) : $"upload-{uploadId}";
        var contentType = MimeHelper.GetMimeType(resolvedFileName);

        var chunkSize = (int)Math.Min(ChunkSize, int.MaxValue);
        var totalBytes = file.CanSeek ? file.Length : -1;
        var totalChunks = totalBytes > 0 ? (int)Math.Ceiling(totalBytes / (double)ChunkSize) : 0;

        var buffer = new byte[chunkSize];
        var chunkIndex = 1;
        long transmitted = 0;
        var started = Stopwatch.StartNew();

        if (file.CanSeek)
        {
            file.Seek(0, SeekOrigin.Begin);
        }

        var crcState = Crc32Helper.Begin();

        int bytesRead;
        while ((bytesRead = await file.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken)) > 0)
        {
            var chunkBytes = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, chunkBytes, 0, bytesRead);

            crcState = Crc32Helper.Update(crcState, chunkBytes);

            using var memoryStream = new MemoryStream(chunkBytes, writable: false);
            using var content = new StreamContent(memoryStream);
            using var formData = new MultipartFormDataContent();

            formData.Add(content, "File", resolvedFileName);
            formData.Add(new StringContent(uploadId), "Payload.UploadId");
            formData.Add(new StringContent(resolvedFileName), "Payload.FileName");
            formData.Add(new StringContent(contentType), "Payload.ContentType");
            formData.Add(new StringContent((totalBytes > 0 ? totalBytes : 0).ToString()), "Payload.FileSize");
            formData.Add(new StringContent(chunkIndex.ToString()), "Payload.ChunkIndex");
            formData.Add(new StringContent(bytesRead.ToString()), "Payload.ChunkSize");
            formData.Add(new StringContent(totalChunks.ToString()), "Payload.TotalChunks");

            var response = await httpClient.PostAsync(uploadApiUrl, formData, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result<uint>.Fail(response.StatusCode, message);
            }

            transmitted += bytesRead;
            var progressFraction = totalBytes > 0
                ? Math.Min((double)transmitted / totalBytes, 1d)
                : 0d;
            onProgressChanged?.Invoke(progressFraction * 100d);

            var elapsed = started.Elapsed;
            var speed = elapsed.TotalSeconds > 0 ? transmitted / elapsed.TotalSeconds : transmitted;
            var remaining = progressFraction > 0 && totalBytes > 0
                ? TimeSpan.FromSeconds((totalBytes - transmitted) / speed)
                : TimeSpan.Zero;

            OnProgressStatusChanged?.Invoke(this, new ProgressStatus(
                resolvedFileName,
                (float)progressFraction,
                totalBytes,
                transmitted,
                elapsed,
                remaining,
                $"{speed:F2} B/s"));

            chunkIndex++;
        }

        var completePayload = new ChunkUploadCompleteRequestDto
        {
            UploadId = uploadId,
            FileName = resolvedFileName,
            ContentType = contentType,
            Directory = null,
            Metadata = null,
            CommitToStorage = true,
            KeepMergedFile = false
        };

        var mergeResult = await httpClient.PostAsJsonAsync(completeApiUrl, completePayload, cancellationToken);
        if (!mergeResult.IsSuccessStatusCode)
        {
            var message = await mergeResult.Content.ReadAsStringAsync(cancellationToken);
            return Result<uint>.Fail(mergeResult.StatusCode, message);
        }

        var completionJson = await mergeResult.Content.ReadAsStringAsync(cancellationToken);
        using var jsonDocument = JsonDocument.Parse(completionJson);
        var root = jsonDocument.RootElement;

        if (!root.TryGetProperty("isSuccess", out var successElement) || !successElement.GetBoolean())
        {
            if (root.TryGetProperty("problem", out var problemElement))
            {
                var title = problemElement.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : "Chunk upload completion failed";
                return Result<uint>.Fail(title ?? "Chunk upload completion failed");
            }

            return Result<uint>.Fail("Chunk upload completion failed");
        }

        if (!root.TryGetProperty("value", out var valueElement))
        {
            return Result<uint>.Fail("Chunk upload completion response is missing the value payload");
        }

        uint checksum;

        switch (valueElement.ValueKind)
        {
            case JsonValueKind.Number:
                checksum = valueElement.GetUInt32();
                break;
            case JsonValueKind.Object:
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<ChunkUploadCompleteResponseDto>(valueElement.GetRawText());
                    if (dto == null)
                    {
                        return Result<uint>.Fail("Chunk upload completion response is empty");
                    }

                    checksum = dto.Checksum;
                    break;
                }
                catch (JsonException ex)
                {
                    return Result<uint>.Fail(ex);
                }
            }
            case JsonValueKind.String when uint.TryParse(valueElement.GetString(), out var parsed):
                checksum = parsed;
                break;
            default:
                return Result<uint>.Fail("Chunk upload completion response could not be parsed");
        }

        var computedChecksum = Crc32Helper.Complete(crcState);
        var finalChecksum = checksum;

        if (checksum == 0 && computedChecksum != 0)
        {
            finalChecksum = computedChecksum;
        }
        else if (checksum != 0 && checksum != computedChecksum)
        {
            finalChecksum = computedChecksum;
        }

        return Result<uint>.Succeed(finalChecksum);
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

file class ChunkUploadCompleteRequestDto
{
    public string UploadId { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? Directory { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public bool CommitToStorage { get; set; }
    public bool KeepMergedFile { get; set; }
}

file class ChunkUploadCompleteResponseDto
{
    public uint Checksum { get; set; }
    public BlobMetadata? Metadata { get; set; }
}
