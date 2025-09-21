using System.IO;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Server.Extensions;
using ManagedCode.Storage.Tests.Common.TestApp.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Common.TestApp;

public class HttpHostProgram
{
    public static void Main(string[] args)
    {
        var options = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = Directory.GetCurrentDirectory()
        };
        var builder = WebApplication.CreateBuilder(options);

        builder.Services.AddControllers();
        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 8L * 1024 * 1024; // 8 MB
        });
        builder.Services.AddEndpointsApiExplorer();

        // Configure form options for large file uploads
        builder.Services.Configure<FormOptions>(options =>
        {
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartBodyLengthLimit = long.MaxValue;
            options.MultipartHeadersLengthLimit = int.MaxValue;
        });
        

        var app = builder.Build();

        app.UseRouting();
        app.MapControllers();
        app.MapStorageHub();

        app.Run();
    }
}
