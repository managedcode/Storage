using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TestTask.API.Controllers;

[ApiController]
[Route("[controller]")]
public class BaseController: ControllerBase
{
    protected readonly ILogger<BaseController> _logger;
    protected readonly IMediator _mediator;

    public BaseController(ILogger<BaseController> logger, IMediator mediator)//I prefer to use standard ctor due to its immutability
    {
        _logger = logger;
        _mediator = mediator;
    }
}