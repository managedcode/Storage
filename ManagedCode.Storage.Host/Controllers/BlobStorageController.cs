using ManagedCode.Storage.Core;
using ManagedCode.Storage.WebApi.Common;
using ManagedCode.Storage.WebApi.Common.Attributes;
using ManagedCode.Storage.WebApi.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace ManagedCode.Storage.WebApi.Controllers;

[Route("api/blob-storage")]
[ApiController]
public class BlobStorageController(
    IStorageFactory storageFactory, 
    ILogger<BlobStorageController> logger
    ): ControllerBase
{
    /// <summary>
    /// Download file from storage by file name
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="storageType">Type of storage. Possible values are:
    /// <see cref="StorageType.Aws"/>,
    /// <see cref="StorageType.Azure"/>,
    /// <see cref="StorageType.DataLake"/>,
    /// <see cref="StorageType.FileSystem"/>,
    /// <see cref="StorageType.Google"/>
    /// </param>
    [HttpGet("download/{storageType}/{fileName}")]
    public async Task<IActionResult> DownloadFile(string fileName, StorageType storageType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("File name cannot be empty.");
            }

            var storage = storageFactory.GetStorage(storageType);
            var result = await storage.DownloadAsync(fileName).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                logger.LogError("File [{FileName}] from [{Storage}] downloading failed with error: {Error}", 
                    fileName, storageType.ToString(), result.GetError()?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, result.GetError()?.Message);
            }
            
            return File(result.Value.FileStream, result.Value.BlobMetadata?.MimeType ?? "application/octet-stream", result.Value.Name);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Cannot download file");
            return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
        }
    }
    
    /// <summary>
    /// Delete file from storage by file name
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="storageType">Type of storage. Possible values are:
    /// <see cref="StorageType.Aws"/>,
    /// <see cref="StorageType.Azure"/>,
    /// <see cref="StorageType.DataLake"/>,
    /// <see cref="StorageType.FileSystem"/>,
    /// <see cref="StorageType.Google"/>
    /// </param>
    [HttpDelete("delete/{storageType}/{fileName}")]
    public async Task<IActionResult> DeleteFile(string fileName, StorageType storageType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("File name cannot be empty.");
            }

            var storage = storageFactory.GetStorage(storageType);
            var result = await storage.DeleteAsync(fileName).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                logger.LogError("File [{FileName}] from [{Storage}] deletion failed with error: {Error}", 
                    fileName, storageType.ToString(), result.GetError()?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, result.GetError()?.Message);
            }
            
            return Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Cannot delete file");
            return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
        }
    }
    
    /// <summary>
    /// Upload file via streaming, but with con in a way that it is impossible to do via swagger
    /// Swagger thing can be fixed if we use IFormFile, but it buffer files, so for large data it is not advisable
    /// </summary>
    /// <param name="request">File</param>
    /// <param name="storageType">Type of storage. Possible values are:
    /// <see cref="StorageType.Aws"/>,
    /// <see cref="StorageType.Azure"/>,
    /// <see cref="StorageType.DataLake"/>,
    /// <see cref="StorageType.FileSystem"/>,
    /// <see cref="StorageType.Google"/>
    /// </param>
    [HttpPost("upload/{storageType}")]
    [Consumes("multipart/form-data")]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> UploadFile(StorageType storageType)
    {
        try
        {
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType), 128);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();
            await using var ms = new MemoryStream();
            string? fileName = null;
            while (section != null)
            {
                var contentDisposition = ContentDispositionHeaderValue.Parse(
                    section.ContentDisposition);
                if (contentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(contentDisposition.FileName.ToString()))
                {
                    fileName = contentDisposition.FileName.ToString();
                    await section.Body.CopyToAsync(ms);
                }
        
                section = await reader.ReadNextSectionAsync();
            }
            
            if (ms.Length is 0 || fileName is null)
            {
                return BadRequest("File is empty or not provided");
            }
            
            var storage = storageFactory.GetStorage(storageType);
            var result = await storage.UploadAsync(ms, options =>
            {
                options.FileName = fileName;
            }).ConfigureAwait(false);
        
            if (!result.IsSuccess)
            {
                logger.LogError("File [{FileName}] upload unsuccessfully to [{Storage}] with error: {Error}", 
                    fileName, storageType.ToString(), result.GetError()?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, result.GetError()?.Message);
            }
            
            return Ok(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Cannot upload file");
            return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
        }
    }
    
    /// <summary>
    /// Stream file from storage by file name
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="storageType">Type of storage. Possible values are:
    /// <see cref="StorageType.Aws"/>,
    /// <see cref="StorageType.Azure"/>,
    /// <see cref="StorageType.DataLake"/>,
    /// <see cref="StorageType.FileSystem"/>,
    /// <see cref="StorageType.Google"/>
    /// </param>
    [HttpGet("stream/{storageType}/{fileName}")]
    public async Task<IActionResult> StreamFile(string fileName, StorageType storageType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("File name cannot be empty.");
            }

            var storage = storageFactory.GetStorage(storageType);
            var metaDataResult = await storage.GetBlobMetadataAsync(fileName).ConfigureAwait(false);
            if (!metaDataResult.IsSuccess)
            {
                logger.LogError("File [{FileName}] from [{Storage}] streaming failed with error: {Error}", 
                    fileName, storageType.ToString(), metaDataResult.GetError()?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, metaDataResult.GetError()?.Message);
            }
            
            var fileResult = await storage.GetStreamAsync(fileName).ConfigureAwait(false);
            if (!fileResult.IsSuccess)
            {
                logger.LogError("File [{FileName}] from [{Storage}] streaming failed with error: {Error}", 
                    fileName, storageType.ToString(), fileResult.GetError()?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, fileResult.GetError()?.Message);
            }
            
            return new FileStreamResult(fileResult.Value, metaDataResult.Value.MimeType ?? "application/octet-stream");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Cannot stream file");
            return StatusCode(StatusCodes.Status500InternalServerError, "Something went wrong");
        }
    }
}