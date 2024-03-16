using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load configuration settings from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json");

// Create a folder path for storing files
string storageFolderPath = Path.Combine(Environment.CurrentDirectory, "Files_Storage");

// Check if the storage folder exists, if not, create it
if (!Directory.Exists(storageFolderPath))
{
    Directory.CreateDirectory(storageFolderPath);
}

// Configure storage options to use the created folder
builder.Services.AddFileSystemStorageAsDefault(opt =>
{
    opt.BaseFolder = storageFolderPath;
});

// Add services to the container.

// Add HttpClient service
builder.Services.AddHttpClient();

// Register the FileSystemStorage implementation of the IStorage interface
builder.Services.AddTransient<IStorage, FileSystemStorage>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
