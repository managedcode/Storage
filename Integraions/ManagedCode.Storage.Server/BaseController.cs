// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.IO;
// using System.Linq;
// using System.Net;
// using System.Runtime.CompilerServices;
// using System.Threading;
// using System.Threading.Tasks;
// using ManagedCode.Communication;
// using ManagedCode.Storage.Core;
// using ManagedCode.Storage.Core.Models;
// using Microsoft.AspNetCore.Components.Forms;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Http.Features;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Mvc.Filters;
// using Microsoft.AspNetCore.Mvc.ModelBinding;
// using Microsoft.AspNetCore.WebUtilities;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Options;
// using Microsoft.Net.Http.Headers;
//
// namespace ManagedCode.Storage.Server;
// //implement recommendations from https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-8.0#upload-large-files-with-streaming
// public abstract class BaseStorageController(IStorage storage, IOptions<FormOptions> formOptions) : ControllerBase
// {
//     protected readonly IStorage Storage = storage;
//
//     [HttpPost]
//     [DisableFormValueModelBinding]
//     [ValidateAntiForgeryToken]
//     public async Task<IActionResult> UploadLargeFile()
//     {
//         if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
//         {
//             ModelState.AddModelError("File", "The request couldn't be processed (Error 1).");
//             return BadRequest(ModelState);
//         }
//         
//         var boundary = MultipartRequestHelper.GetBoundary(
//             MediaTypeHeaderValue.Parse(Request.ContentType),
//             formOptions.Value.MultipartBoundaryLengthLimit);
//         var reader = new MultipartReader(boundary, HttpContext.Request.Body);
//
//         var section = await reader.ReadNextSectionAsync();
//         while (section != null)
//         {
//             var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
//                 section.ContentDisposition, out var contentDisposition);
//
//             if (hasContentDispositionHeader)
//             {
//                 if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
//                 {
//                     var fileBytes = await FileHelpers.ProcessStreamedFile(section, contentDisposition,
//                         ModelState, _permittedExtensions, _fileSizeLimit);
//
//                     if (!ModelState.IsValid)
//                     {
//                         return BadRequest(ModelState);
//                     }
//
//                     var result = await UploadFile(fileBytes, "apiUrl", "contentName");
//                     if (result.IsSuccess)
//                     {
//                         return Ok(result);
//                     }
//                     else
//                     {
//                         return StatusCode((int)result.Error, result);
//                     }
//                 }
//             }
//
//             section = await reader.ReadNextSectionAsync();
//         }
//
//         return BadRequest(new { Message = "The request couldn't be processed." });
//     }
//
//     protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IBrowserFile formFile, UploadOptions? options = null)
//     {
//         return await Storage.UploadToStorageAsync(formFile, options);
//     }
//
//     protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IBrowserFile formFile, Action<UploadOptions> options)
//     {
//         return await Storage.UploadToStorageAsync(formFile, options);
//     }
//
//     protected async Task<Result<FileResult>> DownloadAsFileResult(string blobName, CancellationToken cancellationToken = default)
//     {
//         return await Storage.DownloadAsFileResult(blobName, cancellationToken);
//     }
//
//     protected async Task<Result<FileResult>> DownloadAsFileResult(BlobMetadata blobMetadata, CancellationToken cancellationToken = default)
//     {
//         return await Storage.DownloadAsFileResult(blobMetadata, cancellationToken);
//     }
//
//     protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IFormFile formFile, UploadOptions? options = null,
//         CancellationToken cancellationToken = default)
//     {
//         return await Storage.UploadToStorageAsync(formFile, options, cancellationToken);
//     }
//
//     protected async Task<Result<BlobMetadata>> UploadToStorageAsync(IFormFile formFile, Action<UploadOptions> options,
//         CancellationToken cancellationToken = default)
//     {
//         return await Storage.UploadToStorageAsync(formFile, options, cancellationToken);
//     }
//
//     protected async IAsyncEnumerable<Result<BlobMetadata>> UploadToStorageAsync(IFormFileCollection formFiles, UploadOptions? options = null,
//         [EnumeratorCancellation] CancellationToken cancellationToken = default)
//     {
//         foreach (var formFile in formFiles)
//             yield return await Storage.UploadToStorageAsync(formFile, options, cancellationToken);
//     }
//
//     protected async IAsyncEnumerable<Result<BlobMetadata>> UploadToStorageAsync(IFormFileCollection formFiles, Action<UploadOptions> options,
//         [EnumeratorCancellation] CancellationToken cancellationToken = default)
//     {
//         foreach (var formFile in formFiles)
//             yield return await Storage.UploadToStorageAsync(formFile, options, cancellationToken);
//     }
//     
//     
//     public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
//     {
//         public void OnResourceExecuting(ResourceExecutingContext context)
//         {
//             var factories = context.ValueProviderFactories;
//             factories.RemoveType<FormValueProviderFactory>();
//             factories.RemoveType<FormFileValueProviderFactory>();
//             factories.RemoveType<JQueryFormValueProviderFactory>();
//         }
//
//         public void OnResourceExecuted(ResourceExecutedContext context)
//         {
//         }
//     }
//
//     public static class MultipartRequestHelper
//     {
//         public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
//         {
//             var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
//
//             if (string.IsNullOrWhiteSpace(boundary))
//             {
//                 throw new InvalidDataException("Missing content-type boundary.");
//             }
//
//             if (boundary.Length > lengthLimit)
//             {
//                 throw new InvalidDataException(
//                     $"Multipart boundary length limit {lengthLimit} exceeded.");
//             }
//
//             return boundary;
//         }
//
//         public static bool IsMultipartContentType(string? contentType)
//         {
//             return !string.IsNullOrEmpty(contentType)
//                    && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
//         }
//
//         public static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
//         {
//             return contentDisposition != null
//                 && contentDisposition.DispositionType.Equals("form-data")
//                 && string.IsNullOrEmpty(contentDisposition.FileName.Value)
//                 && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
//         }
//
//         public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
//         {
//             return contentDisposition != null
//                 && contentDisposition.DispositionType.Equals("form-data")
//                 && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
//                     || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
//         }
//     }
//
// public static class FileHelpers
// {
//     public static async Task<byte[]> ProcessStreamedFile(
//         MultipartSection section, 
//         ContentDispositionHeaderValue contentDisposition,
//         ModelStateDictionary modelState, 
//         string[] permittedExtensions, 
//         long sizeLimit)
//     {
//         var fileName = WebUtility.HtmlEncode(Path.GetFileName(contentDisposition.FileName.Value));
//         var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
//
//         if (string.IsNullOrEmpty(fileExtension) || !permittedExtensions.Contains(fileExtension))
//         {
//             modelState.AddModelError("File", $"Invalid file extension {fileExtension}, permitted extensions are: {string.Join(", ", permittedExtensions)}");
//             return Array.Empty<byte>();
//         }
//
//         if (section.Body.Length > sizeLimit)
//         {
//             modelState.AddModelError("File", $"The file exceeds the maximum allowed size of {sizeLimit} bytes.");
//             return Array.Empty<byte>();
//         }
//
//         using var memoryStream = new MemoryStream();
//         await section.Body.CopyToAsync(memoryStream);
//
//         if (memoryStream.Length == 0)
//         {
//             modelState.AddModelError("File", "The file is empty.");
//             return Array.Empty<byte>();
//         }
//
//         return memoryStream.ToArray();
//     }
// }
// }
//
// /*
// public class Program
//    {
//        private static readonly HttpClient client = new HttpClient();
//    
//        public static async Task Main(string[] args)
//        {
//            var filePath = "path_to_your_file";
//            var apiUrl = "http://your_api_url/UploadLargeFile";
//    
//            using var stream = File.OpenRead(filePath);
//            using var content = new StreamContent(stream);
//            using var formData = new MultipartFormDataContent();
//    
//            formData.Add(content, "file", Path.GetFileName(filePath));
//    
//            var response = await client.PostAsync(apiUrl, formData);
//    
//            if (response.IsSuccessStatusCode)
//            {
//                Console.WriteLine("File uploaded successfully.");
//            }
//            else
//            {
//                Console.WriteLine($"Failed to upload file. Status code: {response.StatusCode}");
//            }
//        }
//    }*/