﻿using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.IntegrationTests.TestApp.Controllers;

[ApiController]
public class BaseFileController : ControllerBase
{
    private readonly IStorage _storage;

    public BaseFileController(IStorage storage)
    {
        _storage = storage;
    }
    
    [HttpPost("upload")]
    public async Task<Result<BlobMetadata>> UploadFile([FromBody] MultipartFormDataContent content, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _storage.UploadAsync(await content.ReadAsStreamAsync(cancellationToken), cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            return Result.Fail();
        }
    }
    
    [HttpGet("download/{fileName}")]
    public async Task<Stream> DownloadFile([FromQuery] string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _storage.DownloadAsync(fileName, cancellationToken);

            if (result.Value is null)
            {
                return null;
            }
            else
            {
                return result.Value.FileStream;
            }
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}