using System.IO;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Server.Extensions;
using ManagedCode.Storage.Tests.Common.TestApp.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Common.TestApp;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

public class HttpHostProgram
{
    public static Microsoft.Extensions.Hosting.IHostBuilder CreateHostBuilder(string[] args)
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(web => web
                .ConfigureServices(services =>
                {
                    services.AddControllers();
                    services.AddSignalR(options =>
                    {
                        options.EnableDetailedErrors = true;
                        options.MaximumReceiveMessageSize = 8L * 1024 * 1024;
                    });
                    services.Configure<FormOptions>(options =>
                    {
                        options.ValueLengthLimit = int.MaxValue;
                        options.MultipartBodyLengthLimit = long.MaxValue;
                        options.MultipartHeadersLengthLimit = int.MaxValue;
                    });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapStorageHub();
                    });
                }));
    }

    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();
}
