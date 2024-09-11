using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Net;
using ManagedCode.Storage.Client.Helpers;
using ManagedCode.Storage.Client.Extensions;
using ManagedCode.Storage.Client.Resources;

namespace ManagedCode.Storage.Client.Controllers
{
    public abstract class BaseStorageController(IStorage storage, IOptions<FormOptions> formOptions) : ControllerBase
    {
        protected readonly IStorage Storage = storage;

        /// <summary>
        /// Downloads a file from the storage service by its file name.
        /// </summary>
        /// <param name="fileName">The name of the file to download. It must not be null or empty.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the result of the file download. If the file is found and successfully retrieved, it returns the file as a file result.
        /// If the file is not found or if there is an issue with retrieval, it returns HTTP 400 Bad Request or HTTP 404 Not Found accordingly.
        /// </returns>
        /// <response code="200">The file was successfully retrieved and is returned as a file result.</response>
        /// <response code="400">The file name was null or empty.</response>
        /// <response code="404">The file was not found.</response>
        [HttpGet("{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest(ErrorMessages.FileNameNullOrEmpty);
            }

            var result = await DownloadAsFileResult(fileName);

            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(ErrorMessages.FileNotFound);
            }

            return result.Value;
        }

        /// <summary>
        /// Handles large file uploads by processing multipart form data.
        /// </summary>
        /// <remarks>
        /// This endpoint processes large file uploads by parsing multipart form data from the request body. It supports large file uploads by streaming the file directly to storage,
        /// bypassing intermediate file storage on the server. The method validates the content type of the request, reads the file sections, and uploads them using the provided storage service.
        /// </remarks>
        /// <returns>
        /// An <see cref="IActionResult"/> indicating the result of the file upload process. If the file is successfully uploaded, it returns an HTTP 200 OK status with the result.
        /// If the file upload fails, it returns an HTTP 500 Internal Server Error with the error messages. If the request is invalid or cannot be processed,
        /// it returns an HTTP 400 Bad Request with an error message.
        /// </returns>
        /// <response code="200">The file was uploaded successfully.</response>
        /// <response code="400">The request couldn't be processed due to invalid content type or other errors.</response>
        /// <response code="500">An internal server error occurred during the file upload process.</response>
        [HttpPost]
        public async Task<IActionResult> UploadLargeFile()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", ErrorMessages.RequestProcessingError);

                return BadRequest(ModelState);
            }

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                formOptions.Value.MultipartBoundaryLengthLimit);

            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        // var originalFileName = contentDisposition.FileName.Value;
                        var options = new UploadOptions(contentDisposition.FileName.Value);

                        var result = await UploadToStorageAsync(section.Body, options);

                        if (result.IsSuccess)
                        {
                            return Ok(result);
                        }
                        else
                        {
                            return StatusCode((int)HttpStatusCode.InternalServerError, string.Join(' ', result.Errors!.Select(s => s.Message)));
                        }
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            return BadRequest(new { Message = ErrorMessages.RequestProcessingError });
        }

        protected async Task<Result<FileResult>> DownloadAsFileResult(string blobName, CancellationToken cancellationToken = default)
        {
            return await Storage.DownloadAsFileResult(blobName, cancellationToken);
        }

        protected async Task<Result<BlobMetadata>> UploadToStorageAsync(Stream stream, UploadOptions options, CancellationToken cancellationToken = default)
        {
            return await Storage.UploadToStorageAsync(stream, options, cancellationToken);
        }
    }
}