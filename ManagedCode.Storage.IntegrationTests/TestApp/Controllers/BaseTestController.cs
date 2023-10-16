using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Server;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.IntegrationTests.TestApp.Controllers;

[ApiController]
public abstract class BaseTestController<TStorage> : BaseController
    where TStorage : IStorage
{
    protected BaseTestController(TStorage storage) : base(storage)
    {
        
    }

    [HttpPost("upload")]
    //public async Task<Result<BlobMetadata>> UploadFileAsync([FromBody] IBrowserFile formFile)
    public async Task<Result<BlobMetadata>> UploadFileAsync()
    {
        return Result<BlobMetadata>.Succeed(new BlobMetadata());
    }
}