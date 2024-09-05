using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Large Files Upload Configuration
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = null;
});

// README Storage configuration
builder.Services.AddFileSystemStorageAsDefault(new FileSystemStorageOptions
{
    BaseFolder = Path.Combine(Environment.CurrentDirectory, "default-file-storage")
});

builder.Services.AddFileSystemStorage(new FileSystemStorageOptions
{
    BaseFolder = Path.Combine(Environment.CurrentDirectory, "file-storage")
});

//endpoints
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

// Video Storage Configuration
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "video-file-storage")),
    RequestPath = "/video-file-storage"
});

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.MapRazorPages();

app.Run();
