using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Tests.Common.TestApp.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.Tests.Common.TestApp.Controllers;

[Route("azure")]
[ApiController]
public class AzureTestController(IAzureStorage storage) : BaseTestController<IAzureStorage>(storage);