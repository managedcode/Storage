using ManagedCode.Storage.Aws;
using Microsoft.AspNetCore.Mvc;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Communication;

namespace WebApiSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AWSStorageController : Controller
{
    private readonly IAWSStorage _awsStorage;

    public AWSStorageController(IAWSStorage awsStorage)
    {
        _awsStorage = awsStorage;
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadAsync(string fileName)
    {
        var result = await _awsStorage.DownloadAsync(fileName);
        if(result.IsSuccess && result.Value != null)
        {
            return File(result.Value.FileStream, "applocation/octet-stream", fileName);
        }
        return NotFound($"File '{fileName}' not found.");
    }

    [HttpPost("upload")] 
    public async Task<IActionResult> UploadAsync(IFormFile file)
    {
        if(file == null || file.Length == 0)
        {
            return BadRequest($"file is not specified or empty.");
        }

        var fileName = Path.GetFileName(file.FileName);
        using (var stream = file.OpenReadStream())
        {
            var result = await _awsStorage.UploadAsync(stream);
            if(result.IsSuccess)
            {
                return Ok($"File '{fileName}' uploaded successfully.");
            }
        }

        return BadRequest("Error uploading file.");
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAsync(string fileName)
    {
        var result = await _awsStorage.DeleteAsync(fileName);

        if(result.IsSuccess)
        {
            return Ok($"File '{fileName}' deleted successfully.");
        }

        return NotFound($"File '{fileName}' not found or error deleting.");
    }
}