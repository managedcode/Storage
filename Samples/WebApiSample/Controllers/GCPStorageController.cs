using ManagedCode.Storage.Gcp;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GCPStorageController : Controller
{
    private readonly IGCPStorage _gcpStorage;

    public GCPStorageController(IGCPStorage gcpStorage)
    {
        _gcpStorage = gcpStorage;
    }
}