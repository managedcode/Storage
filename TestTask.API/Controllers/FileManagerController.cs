using Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TestTask.Core.Commands;
using TestTask.Core.Queries;

namespace TestTask.API.Controllers;

/// <summary>
/// The FileManagerController provides endpoints to upload, download, and delete files from storage providers.
/// It leverages MediatR to handle the corresponding commands and queries.
/// </summary>
[ApiController]
[Route("[controller]")]
public class FileManagerController : BaseController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileManagerController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <param name="mediator">The mediator instance to send commands and queries.</param>
    public FileManagerController(ILogger<BaseController> logger, IMediator mediator) 
        : base(logger, mediator)
    {
    }

    /// <summary>
    /// Uploads a file to the specified storage provider.
    /// </summary>
    /// <param name="file">The file to be uploaded.</param>
    /// <param name="provider">The storage provider type (AWS, GCP, Azure, FileStorage).</param>
    /// <param name="fileName">The name to be assigned to the uploaded file.</param>
    /// <returns>
    /// A <see cref="IActionResult"/> indicating the result of the file upload.
    /// Returns <see cref="OkObjectResult"/> with the file metadata if the upload is successful.
    /// Returns <see cref="BadRequestObjectResult"/> if no file is provided.
    /// Returns <see cref="StatusCodeResult"/> with a 500 status code for server errors.
    /// </returns>
    [HttpPost("{provider}/{fileName}")]
    public async Task<IActionResult> UploadFile(IFormFile file, ProviderType provider, string fileName)
    {
        if (file is null || file.Length == 0)
        {
            _logger.LogError("File upload failed: no file provided.");
            return BadRequest("No file uploaded.");
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadFileCommand(stream, fileName, provider);
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            _logger.LogInformation("File {FileName} uploaded successfully.", fileName);
            return Ok(result.Value);
        }

        _logger.LogError("File upload failed: {Error}", result.GetError());
        return StatusCode(500, result.GetError());
    }

    /// <summary>
    /// Downloads a file from the specified storage provider.
    /// </summary>
    /// <param name="provider">The storage provider type (AWS, GCP, Azure, FileStorage).</param>
    /// <param name="fileName">The name of the file to be downloaded.</param>
    /// <returns>
    /// A <see cref="IActionResult"/> that returns the file stream if the download is successful.
    /// Returns <see cref="FileResult"/> for a successful file download with the file content as "application/octet-stream".
    /// Returns <see cref="StatusCodeResult"/> with a 500 status code for server errors.
    /// </returns>
    [HttpGet("{provider}/{fileName}")]
    public async Task<IActionResult> GetFile(ProviderType provider, string fileName)
    {
        var command = new DownloadFileQuery(fileName, provider);
        var result = await _mediator.Send(command);

        if (result is { IsSuccess: true, Value: not null })
        {
            _logger.LogInformation("File {FileName} downloaded successfully.", fileName);
            return File(result.Value.FileStream, "application/octet-stream", fileName);
        }

        _logger.LogError("File download failed: {Error}", result.GetError());
        return StatusCode(500, result.GetError());
    }

    /// <summary>
    /// Deletes a file from the specified storage provider.
    /// </summary>
    /// <param name="provider">The storage provider type (AWS, GCP, Azure, FileStorage).</param>
    /// <param name="fileName">The name of the file to be deleted.</param>
    /// <returns>
    /// A <see cref="IActionResult"/> indicating the result of the file deletion.
    /// Returns <see cref="NoContentResult"/> if the deletion is successful.
    /// Returns <see cref="StatusCodeResult"/> with a 500 status code for server errors.
    /// </returns>
    [HttpDelete("{provider}/{fileName}")]
    public async Task<IActionResult> DeleteFile(ProviderType provider, string fileName)
    {
        var command = new DeleteFileCommand(fileName, provider);
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            _logger.LogInformation("File {FileName} deleted successfully.", fileName);
            return NoContent();
        }

        _logger.LogError("File deletion failed: {Error}", result.GetError());
        return StatusCode(500, result.GetError());
    }
}
