using System.Net;
using ManagedCode.Storage.FileSystem;
using Microsoft.AspNetCore.Mvc;

namespace BlobStorageAccessApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileSystemBlobStorageController(
    IFileSystemStorage fileSystemStorage,
    ILogger<FileSystemBlobStorageController> logger)
    : ControllerBase
{
    private readonly IFileSystemStorage _fileSystemStorage = fileSystemStorage ?? throw new ArgumentNullException(nameof(fileSystemStorage));
    private readonly ILogger<FileSystemBlobStorageController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Uploads a file to the file system storage.
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile formFile)
    {
        if (formFile.Length == 0) return BadRequest("No file provided.");

        try
        {
            await using var stream = formFile.OpenReadStream();

            var result = await _fileSystemStorage.UploadAsync(stream, options =>
                options.FileName = formFile.FileName);

            if (result.IsSuccess)
            {
                return Ok(result);
            }

            _logger.LogError($"Failed to upload file: {result.GetError()?.Message}");
            return Problem(result.GetError()?.Message, result.GetError()?.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while uploading the file.");
            return Problem("An error occurred while uploading the file.",
                statusCode: (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    ///     Downloads a file from the file system storage.
    /// </summary>
    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return BadRequest("File name is required.");

        try
        {
            var result = await _fileSystemStorage.DownloadAsync(fileName);

            if (result.IsSuccess)
            {
                var localFile = result.Value;
                var stream = localFile.FileStream;

                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{localFile.Name}\"";
                Response.Headers["Cache-Control"] = "no-cache";
                Response.Headers["Content-Length"] = localFile.FileInfo.Length.ToString();

                return File(stream, "application/octet-stream");
            }

            _logger.LogError($"Failed to download file: {result.GetError()?.Message}");
            return Problem(result.GetError()?.Message, result.GetError()?.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while downloading the file.");
            return Problem("An error occurred while downloading the file.",
                statusCode: (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>
    ///     Deletes a file from the file system storage.
    /// </summary>
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return BadRequest("File name is required.");

        try
        {
            var result = await _fileSystemStorage.DeleteAsync(fileName);

            if (result.IsSuccess) return Ok(true);

            _logger.LogError($"Failed to delete file: {result.GetError()?.Message}");
            return Problem(result.GetError()?.Message, result.GetError()?.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the file.");
            return Problem("An error occurred while deleting the file.",
                statusCode: (int)HttpStatusCode.InternalServerError);
        }
    }
}