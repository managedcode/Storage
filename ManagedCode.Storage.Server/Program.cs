using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using ManagedCode.Storage.Server.Enums;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.DataLake;
using ManagedCode.Storage.FileSystem;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("current",
    new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ManagedCode.Storage API",
        Version = "v0.66"
    });
});

#region StorageType
var storageType = Convert.ToInt32(builder.Configuration.GetSection("StorageType").Value);
switch ((StorageTypes)storageType)
{
    case StorageTypes.Aws:
        builder.Services.AddTransient<IStorage, AWSStorage>();
        break;
    case StorageTypes.Azure:
        builder.Services.AddTransient<IStorage, AzureStorage>();
        break;
    case StorageTypes.DataLake:
        builder.Services.AddTransient<IStorage, AzureDataLakeStorage>();
        break;
    case StorageTypes.FileSystem:
        builder.Services.AddTransient<IStorage, FileSystemStorage>();
        break;
    case StorageTypes.Google:
        //TODO: issue with reference to google storage
        break;
}
#endregion


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/current/swagger.json", "ManagedCode.Storage API");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
