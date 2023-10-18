using Amazon.Runtime.Internal;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.IntegrationTests.TestApp.Controllers.Base;

[ApiController]
public abstract class BaseTestController<TStorage> : BaseController
    where TStorage : IStorage
{
    private readonly ResponseContext _responseData;
    private readonly int chunkSize;
    private readonly string tempFolder;

    protected BaseTestController(TStorage storage) : base(storage)
    {
        _responseData = new ResponseContext();
        chunkSize = 100000000;
        tempFolder = "C:\\Users\\sasha";
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
    
    
    //create file
    //upload chunks
    //check file
    
    [HttpPost("upload-chunks")]
    public async Task<IActionResult> UploadChunks(CancellationToken cancellationToken)
    {
        try
        {
            var chunkNumber = Guid.NewGuid().ToString();
            string newpath = Path.Combine(tempFolder + "/TEMP", "file" + chunkNumber);
            
            await using (FileStream fs = System.IO.File.Create(newpath))
            {
                byte[] bytes = new byte[chunkSize];
                int bytesRead = 0;
                while ((bytesRead = await Request.Body.ReadAsync(bytes, 0, bytes.Length, cancellationToken)) > 0)
                {
                    await fs.WriteAsync(bytes, 0, bytesRead, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            // _responseData.Response = ex.Message;
            // _responseData.IsSuccess = false;
        }

        return Ok(_responseData);
    }

    [HttpPost("upload-chunks/complete")]
    public async Task<Result> UploadComplete([FromBody] string fileName)
    {
        try
        {
            string tempPath = tempFolder + "/TEMP";
            string newPath = Path.Combine(tempPath, fileName);
            // string[] filePaths = Directory.GetFiles(tempPath).Where(p => p.Contains(fileName))
            //     .OrderBy(p => Int32.Parse(p.Replace(fileName, "$").Split('$')[1])).ToArray();
            string[] filePaths = Directory.GetFiles(tempPath).Where(p => p.Contains(fileName)).ToArray();
            foreach (string filePath in filePaths)
            {
                MergeChunks(newPath, filePath);
            }

            System.IO.File.Move(Path.Combine(tempPath, fileName), Path.Combine(tempFolder, fileName));
        }
        catch (Exception ex)
        {
            // _responseData.ErrorMessage = ex.Message;
            // _responseData.IsSuccess = false;
        }

        return Result.Succeed();
    }

    private static void MergeChunks(string chunk1, string chunk2)
    {
        FileStream fs1 = null;
        FileStream fs2 = null;
        try
        {
            fs1 = System.IO.File.Open(chunk1, FileMode.Append);
            fs2 = System.IO.File.Open(chunk2, FileMode.Open);
            byte[] fs2Content = new byte[fs2.Length];
            fs2.Read(fs2Content, 0, (int)fs2.Length);
            fs1.Write(fs2Content, 0, (int)fs2.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + " : " + ex.StackTrace);
        }
        finally
        {
            if (fs1 != null) fs1.Close();
            if (fs2 != null) fs2.Close();
            System.IO.File.Delete(chunk2);
        }
    }
}