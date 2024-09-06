using System.Diagnostics;
using System.Text.Json.Serialization;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.WebApi.Services.Abstractions;
using ManagedCode.Storage.WebApi.Services.Implementations;
using Microsoft.AspNetCore.Server.Kestrel.Core;

AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName)!)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

builder.Services.AddFileSystemStorage(new FileSystemStorageOptions
{
    BaseFolder = Path.Combine(Environment.CurrentDirectory, "file-storage")
});

builder.Services.AddTransient<IStorageFactory, StorageFactory>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = null;
});

builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
builder.Services.AddMvcCore();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    Console.WriteLine((e.ExceptionObject as Exception)?.ToString());
}