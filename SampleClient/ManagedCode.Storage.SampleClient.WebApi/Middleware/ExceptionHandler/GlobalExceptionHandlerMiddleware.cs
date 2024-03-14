using System.ComponentModel.DataAnnotations;
using System.Net;
using ManagedCode.Storage.SampleClient.Core.Exceptions.Models;

namespace ManagedCode.Storage.SampleClient.WebApi.Middleware.ExceptionHandler;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Similar handlers could be added to other exception types (ex. if it's needed to return other status codes)
        if (exception is ValidationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new ExceptionResult
            {
                Message = exception.Message
            });
        }
        // Default exception handler with 500 status code
        else 
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new ExceptionResult
            {
                Message = exception.Message
            });
        }
    }
}
