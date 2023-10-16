﻿using ManagedCode.Storage.Azure;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.IntegrationTests.TestApp.Controllers;

[Route("azure")]
[ApiController]
public class AzureTestController : BaseTestController<IAzureStorage>
{
    public AzureTestController(IAzureStorage storage) : base(storage)
    {
    }
}