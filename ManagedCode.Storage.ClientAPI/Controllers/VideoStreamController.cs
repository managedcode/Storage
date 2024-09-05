using Microsoft.AspNetCore.Mvc;

namespace BlobStorageAccessApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoStreamController : ControllerBase
    {
        private readonly string _videoFolderPath;

        public VideoStreamController()
        {
            _videoFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "video-file-storage");
        }

        [HttpGet("stream/{fileName}")]
        public async Task<IActionResult> PlayVideo(string fileName)
        {
            var videoPath = Path.Combine(_videoFolderPath, fileName);

            if (!System.IO.File.Exists(videoPath))
            {
                return NotFound();
            }

            var videoUrl = Url.Action("StreamVideo", new { fileName });

            var htmlFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "play-video.html");
            var htmlContent = await System.IO.File.ReadAllTextAsync(htmlFilePath);

            htmlContent = htmlContent.Replace("{{fileName}}", fileName)
                                     .Replace("{{videoUrl}}", videoUrl);

            return Content(htmlContent, "text/html");
        }

        [HttpGet("video/{fileName}")]
        public IActionResult StreamVideo(string fileName)
        {
            var videoPath = Path.Combine(_videoFolderPath, fileName);

            if (!System.IO.File.Exists(videoPath))
            {
                return NotFound();
            }

            var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return new FileStreamResult(fileStream, "application/octet-stream")
            {
                EnableRangeProcessing = true
            };
        }
    }
}
