using ManagedCode.Storage.FileSystem;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ManagedCode.Storage.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileStorageController : ControllerBase
    {
        private readonly IFileSystemStorage _storage;
        private readonly ILogger<FileStorageController> _log;

        public FileStorageController(IFileSystemStorage storage, ILogger<FileStorageController> log)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Uploads a file to the storage.
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File not provided.");

            try
            {
                await using var inputStream = file.OpenReadStream();
                var uploadResult = await _storage.UploadAsync(inputStream, opt =>
                    opt.FileName = file.FileName);

                if (uploadResult.IsSuccess)
                    return Ok(uploadResult);

                _log.LogError($"Upload failed: {uploadResult.GetError()?.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, uploadResult.GetError()?.Message);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error occurred during file upload.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "An internal error occurred.");
            }
        }

        /// <summary>
        /// Downloads a file from the storage.
        /// </summary>
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFileAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name must be specified.");

            try
            {
                var downloadResult = await _storage.DownloadAsync(fileName);

                if (downloadResult.IsSuccess)
                {
                    var file = downloadResult.Value;
                    var fileStream = file.FileStream;

                    Response.Headers["Content-Disposition"] = $"attachment; filename=\"{file.Name}\"";
                    Response.Headers["Cache-Control"] = "no-cache";
                    Response.Headers["Content-Length"] = file.FileInfo.Length.ToString();

                    return File(fileStream, "application/octet-stream");
                }

                _log.LogError($"Download failed: {downloadResult.GetError()?.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, downloadResult.GetError()?.Message);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error occurred during file download.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "An internal error occurred.");
            }
        }

        /// <summary>
        /// Deletes a file from the storage.
        /// </summary>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteFileAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name must be provided.");

            try
            {
                var deleteResult = await _storage.DeleteAsync(fileName);

                if (deleteResult.IsSuccess)
                    return Ok(true);

                _log.LogError($"Deletion failed: {deleteResult.GetError()?.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, deleteResult.GetError()?.Message);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error occurred during file deletion.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "An internal error occurred.");
            }
        }
    }
}
