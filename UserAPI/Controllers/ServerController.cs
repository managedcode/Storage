using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Net.Http;
using System.Threading;

namespace UserAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class FileController : Controller
{
    private readonly IStorage _storage;
    private const string TempStoragePath = "D:\\temp\\";

    public FileController(IStorage storage)
    {
        _storage = storage;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File not provided or empty.");
        }

        using (var stream = file.OpenReadStream())
        {
            await _storage.UploadAsync(stream);
        }

        return Ok("File uploaded successfully.");
    }



    [HttpPost("upload-chunk")]
    public async Task<IActionResult> UploadChunk([FromForm] IFormFile chunk, [FromForm] int chunkIndex, [FromForm] int totalChunks, CancellationToken cancellationToken)
    {
        if (chunk == null || chunk.Length == 0)
        {
            return BadRequest("Chunk not provided or empty.");
        }

        var tempFilePath = Path.Combine(TempStoragePath, chunk.FileName + $".part_{chunkIndex}");

        using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
        {
            await chunk.CopyToAsync(fileStream);
        }

        if (chunkIndex + 1 == totalChunks)
        {
            await CombineChunks(chunk.FileName, totalChunks, cancellationToken);
        }

        return Ok("Chunk uploaded successfully.");
    }

    private async Task CombineChunks(string fileName, int totalChunks, CancellationToken cancellationToken)
    {
        var finalFilePath = Path.Combine(TempStoragePath, fileName);

        using (var finalFileStream = new FileStream(finalFilePath, FileMode.Create))
        {
            for (int i = 0; i < totalChunks; i++)
            {
                var tempFilePath = Path.Combine(TempStoragePath, fileName + $".part_{i}");

                using (var chunkStream = new FileStream(tempFilePath, FileMode.Open))
                {
                    await chunkStream.CopyToAsync(finalFileStream);
                }

                System.IO.File.Delete(tempFilePath);
            }
        }

        using (var finalFileStream = new FileStream(finalFilePath, FileMode.Open))
        {
            await _storage.UploadAsync(finalFileStream, cancellationToken);
        }

        System.IO.File.Delete(finalFilePath);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File not provided or empty.");
        }

        using (var stream = file.OpenReadStream())
        {
            await _storage.UploadAsync(stream, cancellationToken); 
        }

        return Ok("File uploaded successfully.");
    }    


    // 2. Download a file (GET request)
    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
        var result = await _storage.DownloadAsync(fileName);

        if (!result.IsSuccess || result.Value == null)
        {
            return NotFound("File not found.");
        }

        var localFile = result.Value;
        var stream = localFile.FileStream;

        if (stream == null)
        {
            return NotFound("File stream not found.");
        }

        return File(stream, "application/octet-stream", System.IO.Path.GetFileName(localFile.FilePath));
    }

    // 3. Delete a file (DELETE request)
    [HttpDelete("delete/{fileName}")]
    public async Task<IActionResult> DeleteFile(string fileName)
    {
        await _storage.DeleteAsync(fileName);
        return Ok("File deleted successfully.");
    }

    // 4. Get file metadata (GET request)
    [HttpGet("metadata/{fileName}")]
    public async Task<IActionResult> GetFileMetadata(string fileName)
    {
        var metadata = await _storage.GetBlobMetadataAsync(fileName);

        if (metadata == null)
        {
            return NotFound("Metadata not found.");
        }

        return Ok(metadata);
    }

    // 5. Replace or update a file (PUT request)
    [HttpPut("replace/{fileName}")]
    public async Task<IActionResult> ReplaceFile(string fileName, [FromForm] IFormFile newFile)
    {
        if (newFile == null || newFile.Length == 0)
        {
            return BadRequest("New file not provided or empty.");
        }

        await _storage.DeleteAsync(fileName);

        using (var stream = newFile.OpenReadStream())
        {
            await _storage.UploadAsync(stream);
        }

        return Ok("File replaced successfully.");
    }
}
