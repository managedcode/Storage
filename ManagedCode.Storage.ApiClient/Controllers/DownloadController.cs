using ManagedCode.Storage.Core;
using ManagedCode.Storage.Server;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.ApiClient.Controllers
{
    /// <summary>
    /// Controller for file downloads
    /// </summary>
    [ApiController]
    [Route("api/download")]
    public class DownloadController : ControllerBase
    {
        private readonly IStorage _storage;
        
        /// <summary>
        /// Constructor for DownloadController
        /// </summary>
        /// <param name="storage"> Storage service used for file downloads </param>
        public DownloadController(IStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// Method for downloading a file by its name.
        /// </summary>
        /// <param name="fileName"> The name of the file to download </param>
        /// <returns>The result of the file download as a <see cref="FileResult"/></returns>
        [HttpGet("{fileName}")]
        public async Task<FileResult> DownloadFileAsync(string fileName)
        {
            // Call the file download method from the storage.
            var result = await _storage.DownloadAsFileResult(fileName);
        
            // Check the operation result for any errors.
            result.ThrowIfFail();

            // Return the operation result as a FileResult.
            return result.Value!;
        }
    }
}