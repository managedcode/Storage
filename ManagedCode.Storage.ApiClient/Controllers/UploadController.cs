using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.ApiClient.Controllers
{
    /// <summary>
    /// Controller for file uploads.
    /// </summary>
    [ApiController]
    [Route("api/upload")]
    public class UploadController : ControllerBase
    {
        private readonly IStorage _storage;
        
        /// <summary>
        /// Constructor for UploadController.
        /// </summary>
        /// <param name="storage">Storage service used for file uploads.</param>
        public UploadController(IStorage storage)
        {
            _storage = storage;
        }
        
        /// <summary>
        /// Method for uploading a file.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <returns>The result of the file upload as a <see cref="Result{BlobMetadata}"/>.</returns>
        [HttpPost]
        public async Task<Result<BlobMetadata>> UploadFileAsync(IFormFile file)
        {
            if (Request.HasFormContentType is false)
            {
                return Result<BlobMetadata>.Fail("invalid body");
            }

            return await _storage.UploadAsync(file.OpenReadStream());
        }
        
        /// <summary>
        /// Method for uploading a large file in chunks.
        /// </summary>
        /// <param name="file">The file upload payload containing chunk information.</param>
        /// <returns>The result of the upload operation as a <see cref="Result"/>.</returns>
        [HttpPost("upload-chunks/upload")]
        public async Task<Result> UploadLargeFile(FileUploadPayload file)
        {
            try
            {
                // Create a temporary path for storing the chunk.
                string newpath = Path.Combine(Path.GetTempPath(), $"{file.File.FileName}_{file.Payload.ChunkIndex}");
            
                // Write the chunk data to a temporary file.
                await using (FileStream fs = System.IO.File.Create(newpath))
                {
                    byte[] bytes = new byte[file.Payload.ChunkSize];
                    int bytesRead = 0;
                    var fileStream = file.File.OpenReadStream();
                    while ((bytesRead = await fileStream.ReadAsync(bytes, 0, bytes.Length)) > 0)
                    {
                        await fs.WriteAsync(bytes, 0, bytesRead);
                    }
                }
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }

            return Result.Succeed();
        }
    }
}