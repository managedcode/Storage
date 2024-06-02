using Amazon.Runtime.Internal;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.IntegrationTests.TestApp.Controllers.Base;

[ApiController]
public abstract class BaseTestController<TStorage> : ControllerBase
    where TStorage : IStorage
{
    protected readonly IStorage Storage;
    protected readonly ResponseContext ResponseData;
    protected readonly int ChunkSize;

    protected BaseTestController(TStorage storage)
    {
        Storage = storage;
        ResponseData = new ResponseContext();
        ChunkSize = 100000000;
    }

    [HttpPost("upload")]
    public async Task<Result<BlobMetadata>> UploadFileAsync([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (Request.HasFormContentType is false)
        {
            return Result<BlobMetadata>.Fail("invalid body");
        }

        return await Storage.UploadAsync(file.OpenReadStream(), cancellationToken);
    }

    [HttpGet("download/{fileName}")]
    public async Task<FileResult> DownloadFileAsync([FromRoute] string fileName)
    {
        var result = await Storage.DownloadAsFileResult(fileName);
        
        result.ThrowIfFail();

        return result.Value!;
    }

    [HttpGet("stream/{fileName}")]
    public async Task<Microsoft.AspNetCore.Http.IResult> StreamFileAsync([FromRoute] string fileName)
    {
        var result = await Storage.GetStreamAsync(fileName);
        var metadataAsync = await Storage.GetBlobMetadataAsync(fileName);

        result.ThrowIfFail();
        return Results.Stream(result.Value, metadataAsync.Value?.MimeType ?? "application/octet-stream", fileName);
    }

    [HttpPost("upload-chunks/upload")]
    public async Task<Result> UploadLargeFile([FromForm] FileUploadPayload file, CancellationToken cancellationToken = default)
    {
        try
        {
            string newpath = Path.Combine(Path.GetTempPath(), $"{file.File.FileName}_{file.Payload.ChunkIndex}");
            
            await using (FileStream fs = System.IO.File.Create(newpath))
            {
                byte[] bytes = new byte[file.Payload.ChunkSize];
                int bytesRead = 0;
                var fileStream = file.File.OpenReadStream();
                while ((bytesRead = await fileStream.ReadAsync(bytes, 0, bytes.Length, cancellationToken)) > 0)
                {
                    await fs.WriteAsync(bytes, 0, bytesRead, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }

        return Result.Succeed();
    }

    [HttpPost("upload-chunks/complete")]
    public async Task<Result<uint>> UploadComplete([FromBody] string fileName, CancellationToken cancellationToken = default)
    {
        uint fileCRC = 0;
        try
        {
            string tempPath = Path.GetTempPath();
            string newPath = Path.Combine(tempPath, $"{fileName}_merged");
            string[] filePaths = Directory.GetFiles(tempPath).Where(p => p.Contains(fileName))
                .OrderBy(p => Int32.Parse(p.Split('_')[1])).ToArray();
            
            foreach (string filePath in filePaths)
            {
                await MergeChunks(newPath, filePath, cancellationToken);
            }
            
            fileCRC = Crc32Helper.CalculateFileCRC(newPath);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }

        return Result.Succeed(fileCRC);
    }

    private static async Task MergeChunks(string chunk1, string chunk2, CancellationToken cancellationToken)
    {
        long fileSize = 0;
        FileStream fs1 = null;
        FileStream fs2 = null;
        try
        {
            fs1 = System.IO.File.Open(chunk1, FileMode.Append);
            fs2 = System.IO.File.Open(chunk2, FileMode.Open);
            byte[] fs2Content = new byte[fs2.Length];
            await fs2.ReadAsync(fs2Content, 0, (int)fs2.Length, cancellationToken);
            await fs1.WriteAsync(fs2Content, 0, (int)fs2.Length, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + " : " + ex.StackTrace);
        }
        finally
        {
            fileSize = fs1.Length;
            if (fs1 != null) fs1.Close();
            if (fs2 != null) fs2.Close();
            System.IO.File.Delete(chunk2);
        }
    }
}