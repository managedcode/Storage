using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.IntegrationTests.TestApp;

public class HttpHostProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddSignalR();
        builder.Services.AddEndpointsApiExplorer();
        
        // By default body size 4 mb
        // Full body is 128MB
        // builder.Services.Configure<FormOptions>(x =>  {  
        //     x.ValueLengthLimit = int.MaxValue;
        //     x.MultipartBodyLengthLimit = int.MaxValue;
        //     x.MultipartHeadersLengthLimit = int.MaxValue;
        // });


        var app = builder.Build();
    
        app.MapControllers();

        app.Run();
    }   
}