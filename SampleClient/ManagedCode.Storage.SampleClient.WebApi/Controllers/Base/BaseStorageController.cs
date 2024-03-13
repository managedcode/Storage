using ManagedCode.Storage.SampleClient.Core.DTO.DeleteFile;
using ManagedCode.Storage.SampleClient.Core.DTO.DownloadFile;
using ManagedCode.Storage.SampleClient.Core.DTO.UploadFile;
using ManagedCode.Storage.SampleClient.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace ManagedCode.Storage.SampleClient.WebApi;

[RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
[RequestSizeLimit(int.MaxValue)]
public abstract class BaseStorageController : ControllerBase
{

    [HttpGet("download")]
    public async Task<IResult> DownloadFileAsync([FromQuery] DownloadFileRequest request, [FromServices] IFileStorageService fileStorageService)
    {
        var result = await fileStorageService.DownloadFileResult(request);
        var response = Results.Stream(result.FileStream, !string.IsNullOrEmpty(result.MimeType) ? result.MimeType : "application/octet-stream", fileDownloadName: request.FileName);
        return response;
    }

    [HttpPost("upload")]
    public async Task<IResult> UploadFileAsync(IFormFile formFile, [FromServices] IFileStorageService fileStorageService)
    {
        var uploadFileResult = default(UploadFileResult);
        using(var stream = formFile.OpenReadStream())
        {
            uploadFileResult = await fileStorageService.UploadFileAsync(stream, formFile.ContentType, formFile.FileName);
        }
        return Results.Ok(uploadFileResult.FileName);
    }

    [HttpPost("delete")]
    public async Task<IResult> DeleteFileAsync([FromBody] DeleteFileRequest request, [FromServices] IFileStorageService fileStorageService)
    {
        await fileStorageService.DeleteFileAsync(request);
        return Results.Ok();
    }
}
