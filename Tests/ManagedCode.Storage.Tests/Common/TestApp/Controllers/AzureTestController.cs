using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Server.ChunkUpload;
using ManagedCode.Storage.Tests.Common.TestApp.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.Tests.Common.TestApp.Controllers;

[Route("azure")]
[ApiController]
public class AzureTestController(IAzureStorage storage, ChunkUploadService chunkUploadService)
    : BaseTestController<IAzureStorage>(storage, chunkUploadService);
