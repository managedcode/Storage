using ManagedCode.Storage.Aws;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AWSStorageController : Controller
{
    private readonly IAWSStorage _awsStorage;

    public AWSStorageController(IAWSStorage awsStorage)
    {
        _awsStorage = awsStorage;
    }
    
}