using Azure.Identity;
using Azure.Storage.Blobs;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Web;

namespace UserAPI;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddAzureStorageAsDefault(new AzureStorageOptions
        {
            Container = "{YOUR_CONTAINER_NAME}",
            ConnectionString = "{YOUR_CONNECTION_NAME}",
        });
        builder.Services.AddSingleton<BlobServiceClient>((serviceProvider) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var storageAccountUri = config["StorageAccount:Uri"];
            var accountUri = new Uri(storageAccountUri);
            var azureCredentialOptions = new DefaultAzureCredentialOptions();
            azureCredentialOptions.VisualStudioTenantId = config["VisualStudioTenantId"];
            var credential = new DefaultAzureCredential(azureCredentialOptions);
            var blobServiceClient = new BlobServiceClient(accountUri, credential);
            return blobServiceClient;
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        //        app.MapGet("/stream", async (BlobServiceClient blobServiceClient,
        //HttpRequest req, IConfiguration configuration) =>
        //        {
        //            var video = req.Query["video"];
        //            var containerName = configuration["StorageAccount:ContainerName"];
        //            var container = blobServiceClient.GetBlobContainerClient(containerName);
        //            var blob = container.GetBlobClient(HttpUtility.UrlDecode(video));               
        //            var stream = await blob.OpenReadAsync(); 
        //            return Results.Stream(stream, "video/mp4", enableRangeProcessing: true);
        //        });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=User}/{action=UploadFiles}/{id?}");

        app.Run();
    }
}
