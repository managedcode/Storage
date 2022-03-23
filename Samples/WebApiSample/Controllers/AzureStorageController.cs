using ManagedCode.Storage.Azure;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AzureStorageController : Controller
{
    private readonly IAzureStorage _azureStorage;

    public AzureStorageController(IAzureStorage azureStorage)
    {
        _azureStorage = azureStorage;
    }
}