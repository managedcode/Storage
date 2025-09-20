using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime.Internal;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server.ChunkUpload;
using ManagedCode.Storage.Server;
using ManagedCode.Storage.Server.Extensions;
using ManagedCode.Storage.Server.Extensions.Controller;
using ManagedCode.Storage.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace ManagedCode.Storage.Tests.Common.TestApp.Controllers.Base;

[ApiController]
public abstract class BaseTestController<TStorage> : ControllerBase where TStorage : IStorage
{
    protected readonly int ChunkSize;
    protected readonly IStorage Storage;
    private readonly ChunkUploadService _chunkUploadService;

    protected BaseTestController(TStorage storage, ChunkUploadService chunkUploadService)
    {
        Storage = storage;
        _chunkUploadService = chunkUploadService;
        ChunkSize = 100000000;
    }

    [HttpPost("upload")]
    public async Task<Result<BlobMetadata>> UploadFileAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (Request.HasFormContentType is false)
            return Result<BlobMetadata>.Fail("invalid body");
        
        return await Result.From(() => this.UploadFormFileAsync(Storage, file, cancellationToken:cancellationToken), cancellationToken);
    }

    [HttpGet("download/{fileName}")]
    public async Task<FileResult> DownloadFileAsync([FromRoute] string fileName)
    {
        return await this.DownloadAsFileResultAsync(Storage, fileName);
    }

    [HttpGet("stream/{fileName}")]
    public async Task<IResult> StreamFileAsync([FromRoute] string fileName)
    {
        return await this.DownloadAsStreamAsync(Storage, fileName);
    }

    [HttpGet("download-bytes/{fileName}")]
    public async Task<FileContentResult> DownloadBytesAsync([FromRoute] string fileName)
    {
        return await this.DownloadAsFileContentResultAsync(Storage, fileName);
    }

    [HttpPost("upload-chunks/upload")]
    public async Task<Result> UploadLargeFile([FromForm] FileUploadPayload file, CancellationToken cancellationToken = default)
    {
        return await this.UploadChunkAsync(_chunkUploadService, file, cancellationToken);
    }

    [HttpPost("upload-chunks/complete")]
    public async Task<Result<uint>> UploadComplete([FromBody] ChunkUploadCompleteRequest request, CancellationToken cancellationToken = default)
    {
        var completeResult = await this.CompleteChunkUploadAsync(_chunkUploadService, Storage, request, cancellationToken);
        if (completeResult.IsFailed)
        {
            if (completeResult.Problem is not null)
            {
                return Result<uint>.Fail(completeResult.Problem);
            }

            return Result<uint>.Fail("Chunk upload completion failed");
        }

        Console.Error.WriteLine($"Server computed checksum: {completeResult.Value.Checksum}");
        return Result<uint>.Succeed(completeResult.Value.Checksum);
    }
}
