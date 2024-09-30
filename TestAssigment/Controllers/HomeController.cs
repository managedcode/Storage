using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TestAssigmentClient.Services.Abstraction;

namespace TestAssigment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IFileSystemStorage _storage;
        private readonly ILogger<HomeController> _log;

        public HomeController(IFileSystemStorage storage, ILogger<HomeController> log)
        {
            _storage = storage;
            _log = log;
        }

        /// <summary>
        /// Upload a file
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File not found.");

            try
            {
              
                using var inputStream = file.OpenReadStream();
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
        /// Download a file from the storage
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

                    Response.Headers["Content-Length"] = file.FileInfo.Length.ToString();
                    Response.Headers["Content-Disposition"] = $"attachment; filename=\"{file.Name}\"";
                    Response.Headers["Cache-Control"] = "no-cache";

                    return File(fileStream, "application/octet-stream");
                }

                _log.LogError($"Download failed: {downloadResult.GetError()?.Message}");
                return StatusCode((int)HttpStatusCode.InternalServerError, downloadResult.GetError()?.Message);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error occurred while file download proccess.");
                return StatusCode((int)HttpStatusCode.InternalServerError, "An internal error occurred.");
            }
        }

    }
}