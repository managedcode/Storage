using Amazon;
using Amazon.S3;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Gcp.Extensions;
using ManagedCode.Storage.Gcp.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region Add FileSystemStorage

builder.Services.AddFileSystemStorageAsDefault(new FileSystemStorageOptions
{
    BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket")
});

builder.Services.AddFileSystemStorage(new FileSystemStorageOptions
{
    BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket")
});

#endregion

#region Add AzureStorage

builder.Services.AddAzureStorage(new AzureStorageOptions
{
    Container = "managed-code-bucket",
    ConnectionString =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;"
});

#endregion

#region Add AWSSorage

// AWS library overwrites property values. you should only create configurations this way. 
var awsConfig = new AmazonS3Config
{
    RegionEndpoint = RegionEndpoint.EUWest1,
    ForcePathStyle = true,
    UseHttp = true,
    ServiceURL = "http://localhost:4566" // this is the default port for the aws s3 emulator, must be last in the list
};

builder.Services.AddAWSStorage(opt =>
{
    opt.PublicKey = "localkey";
    opt.SecretKey = "localsecret";
    opt.Bucket = "managed-code-bucket";
    opt.OriginalOptions = awsConfig;
});

#endregion

#region Add GCPStorage

builder.Services.AddGCPStorage(new GCPStorageOptions
{
    BucketOptions = new BucketOptions
    {
        ProjectId = "api-project-0000000000000",
        Bucket = "managed-code-bucket"
    }
});

#endregion

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();