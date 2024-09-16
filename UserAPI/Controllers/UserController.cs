using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Threading;

namespace UserAPI.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("[controller]")]
public class UserController : Controller
{
    private readonly IStorage _storage;

    public UserController(IStorage storage)
    {
        _storage = storage;
    }

    // Create (Upload a file)
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFiles([FromForm] IFormFile file)
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

    // Read (Download a file by name)
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

        return File(stream, "application/octet-stream", localFile.FilePath);
    }


    // Update (Replace or upload a new version of the file)
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

    // Delete (Delete a file by name)
    [HttpDelete("delete/{fileName}")]
    public async Task<IActionResult> DeleteFile(string fileName)
    {
        await _storage.DeleteAsync(fileName);
        return Ok("File deleted successfully.");
    }

    // Get metadata (Get file metadata by name)
    [HttpGet("metadata/{fileName}")]
    public async Task<IActionResult> GetFileMetadata(string fileName)
    {
        var metadata = await _storage.GetBlobMetadataAsync(fileName); 
        if (metadata == null)
        {
            return NotFound("Metadata not found for this file.");
        }

        return Ok(metadata); 
    }
}
