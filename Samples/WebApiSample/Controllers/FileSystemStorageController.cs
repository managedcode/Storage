using ManagedCode.Storage.FileSystem;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileSystemStorageController : Controller
{
    private readonly IFileSystemStorage _fileSystemStorage;

    public FileSystemStorageController(IFileSystemStorage fileSystemStorage)
    {
        _fileSystemStorage = fileSystemStorage;
    }
}