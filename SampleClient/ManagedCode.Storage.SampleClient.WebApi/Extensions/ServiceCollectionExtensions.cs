using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.AzureDataLake;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.Gcp;
using ManagedCode.Storage.SampleClient.Core.Enums;
using ManagedCode.Storage.SampleClient.Core.Services.Interfaces;
using ManagedCode.Storage.SampleClient.Domain.Services;
using ManagedCode.Storage.SampleClient.WebApi.Models;

namespace ManagedCode.Storage.SampleClient.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddScopedCurrentState(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<CurrentState>();
    }

    public static void AddStorageService(this WebApplicationBuilder builder)
    {
        builder.Services.AddFileSystemStorage(options => 
        {
            options.BaseFolder = builder.Configuration.GetValue<string>("FileStorage:BaseFolder");
        });

        builder.Services.AddTransient<IStorage>(serviceProvider => 
        {
            var currentState = serviceProvider.GetRequiredService<CurrentState>();
            return currentState.StorageProvider switch
            {
                StorageProvider.AWS => serviceProvider.GetRequiredService<IAWSStorage>(),
                StorageProvider.Azure => serviceProvider.GetRequiredService<IAzureStorage>(),
                StorageProvider.AzureDataLake => serviceProvider.GetRequiredService<IAzureDataLakeStorage>(),
                StorageProvider.GoogleDrive => serviceProvider.GetRequiredService<IGCPStorage>(),
                StorageProvider.FileSystem => serviceProvider.GetRequiredService<IFileSystemStorage>(),
                _ => serviceProvider.GetRequiredService<IFileSystemStorage>(),
            };
        });

        builder.Services.AddTransient<IFileStorageService, FileStorageService>();
    }
}
