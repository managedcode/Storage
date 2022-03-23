using ManagedCode.Storage.Core;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StorageController : Controller
{
    private readonly IStorage _storage;

    public StorageController(IStorage storage)
    {
        _storage = storage;
    }
}