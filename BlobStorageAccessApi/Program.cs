using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRazorPages();

// Large Files Upload 

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue; 
    options.MultipartBodyLengthLimit = int.MaxValue;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = null; 
});

builder.Services.AddScoped<IFileSystemStorage, FileSystemStorage>();

// Register default FileSystemStorage
builder.Services.AddFileSystemStorageAsDefault(new FileSystemStorageOptions
{
    BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket")
});

// Register FileSystemStorage with custom options
builder.Services.AddFileSystemStorage(new FileSystemStorageOptions
{
    BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket")
});

var app = builder.Build();

// Configure the HTTP request pipeline

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.MapRazorPages();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, "managed-code-bucket")),
    RequestPath = "/managed-code-bucket"
});

app.Run();