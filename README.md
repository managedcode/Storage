![img|300x200](https://raw.githubusercontent.com/managed-code-hub/Storage/main/logo.png)
# ManagedCode.Storage
[![.NET](https://github.com/managed-code-hub/Storage/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managed-code-hub/Storage/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/managed-code-hub/Storage/badge.svg?branch=main&service=github)](https://coveralls.io/github/managed-code-hub/Storage?branch=main)
[![nuget](https://github.com/managed-code-hub/Storage/actions/workflows/nuget.yml/badge.svg?branch=main)](https://github.com/managed-code-hub/Storage/actions/workflows/nuget.yml)
[![CodeQL](https://github.com/managed-code-hub/Storage/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managed-code-hub/Storage/actions/workflows/codeql-analysis.yml)

| Version | Package                                                                                                                               | Description     |
| ------- |---------------------------------------------------------------------------------------------------------------------------------------|-----------------|
|[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Core.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Core) | [ManagedCode.Storage.Core](https://www.nuget.org/packages/ManagedCode.Storage.Core)                                                   | Core            |
|[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.FileSystem.svg)](https://www.nuget.org/packages/ManagedCode.Storage.FileSystem) | [ManagedCode.Storage.FileSystem](https://www.nuget.org/packages/ManagedCode.Storage.FileSystem)                                       | FileSystem         |
|[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Azure.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Azure) | [ManagedCode.Storage.Azure](https://www.nuget.org/packages/ManagedCode.Storage.Azure)                                                 | Azure           |
|[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Aws.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Aws) | [ManagedCode.Storage.Aws](https://www.nuget.org/packages/ManagedCode.Storage.Aws)                                     | AWS             |
|[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Gcp.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Gcp) | [ManagedCode.Storage.Gcp](https://www.nuget.org/packages/ManagedCode.Storage.Gcp)                                         | GCP             |
|[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.AspNetExtensions.svg)](https://www.nuget.org/packages/ManagedCode.Storage.AspNetExtensions) | [ManagedCode.Storage.AspNetExtensions](https://www.nuget.org/packages/ManagedCode.Storage.AspNetExtensions)                                         | AspNetExtensions          |

# Storage
---
## Storage pattern implementation for C#.
A universal storage for working with multiple storage providers:
- Azure 
- Google Cloud
- Amazon
- FileSystem
## General concept 
The library incapsulates all provider specific  to make connection and managing storages as easy as possible. You have as customer just connect the library in your Startup providing necessary connection strings and inject needed interfaces in your services.

## Connection modes
You can connect storage interface in two modes provider-specific and default. In case of default you are restricted with one storage type

### Azure

Default mode connection:

```cs
// Startup.cs
services.AddAzureStorageAsDefault(new AzureStorageOptions
{
    Container = "{YOUR_CONTAINER_NAME}",
    ConnectionString = "{YOUR_CONNECTION_NAME}",
});
```

Using in default mode:

```cs
// MyService.cs
public class MyService
{
    private readonly IStorage _storage;

    public MyService(IStorage storage)
    {
        _storage = storage;
    }
}

```

Provider-specific mode connection:

```cs
// Startup.cs
services.AddAzureStorage(new AzureStorageOptions
{
    Container = "{YOUR_CONTAINER_NAME}",
    ConnectionString = "{YOUR_CONNECTION_NAME}",
});
```

Using in provider-specific mode

```cs
// MyService.cs
public class MyService
{
    private readonly IAzureStorage _azureStorage;

    public MyService(IAzureStorage azureStorage)
    {
        _azureStorage = azureStorage;
    }
}
```
<details>
  <summary>Google Cloud (Click here to expand)</summary>

### Google Cloud 
Default mode connection:
```cs
// Startup.cs
services.AddGCPStorageAsDefault(opt =>
{
    opt.GoogleCredential = GoogleCredential.FromFile("{PATH_TO_YOUR_CREDENTIALS_FILE}.json");

    opt.BucketOptions = new BucketOptions()
    {
        ProjectId = "{YOUR_API_PROJECT_ID}",
        Bucket = "{YOUR_BUCKET_NAME}",
    };
});
```
Using in default mode:
```cs
// MyService.cs
public class MyService
{
    private readonly IStorage _storage;
  
    public MyService(IStorage storage)
    {
        _storage = storage;
    }
}
```
Provider-specific mode connection:
```cs
// Startup.cs
services.AddGCPStorage(new GCPStorageOptions
{
    BucketOptions = new BucketOptions()
    {
        ProjectId = "{YOUR_API_PROJECT_ID}",
        Bucket = "{YOUR_BUCKET_NAME}",
    }
});
```
Using in provider-specific mode
```cs
// MyService.cs
public class MyService
{
    private readonly IGCPStorage _gcpStorage;
    public MyService(IGCPStorage gcpStorage)
    {
        _gcpStorage = gcpStorage;
    }
}
```
</details>

<details>
  <summary>Amazon (Click here to expand)</summary>
  
### Amazon
Default mode connection:
```cs
// Startup.cs
//aws libarary overwrites property values. you should only create configurations this way. 
var awsConfig = new AmazonS3Config();
awsConfig.RegionEndpoint = RegionEndpoint.EUWest1;
awsConfig.ForcePathStyle = true;
awsConfig.UseHttp = true;
awsConfig.ServiceURL = "http://localhost:4566"; //this is the default port for the aws s3 emulator, must be last in the list

services.AddAWSStorageAsDefault(opt =>
{
    opt.PublicKey = "{YOUR_PUBLIC_KEY}";
    opt.SecretKey = "{YOUR_SECRET_KEY}";
    opt.Bucket = "{YOUR_BUCKET_NAME}";
    opt.OriginalOptions = awsConfig;
});
```
Using in default mode:
```cs
// MyService.cs
public class MyService
{
    private readonly IStorage _storage;
  
    public MyService(IStorage storage)
    {
        _storage = storage;
    }
}
```
Provider-specific mode connection:
```cs
// Startup.cs
services.AddAWSStorage(new AWSStorageOptions
{
    PublicKey = "{YOUR_PUBLIC_KEY}",
    SecretKey = "{YOUR_SECRET_KEY}",
    Bucket = "{YOUR_BUCKET_NAME}",
    OriginalOptions = awsConfig
});
```
Using in provider-specific mode
```cs
// MyService.cs
public class MyService
{
    private readonly IAWSStorage _gcpStorage;
    public MyService(IAWSStorage gcpStorage)
    {
        _gcpStorage = gcpStorage;
    }
}
```
</details>

<details>
  <summary>FileSystem (Click here to expand)</summary>
  
### FileSystem
Default mode connection:
```cs
// Startup.cs
services.AddFileSystemStorageAsDefault(opt =>
{
    opt.BaseFolder = Path.Combine(Environment.CurrentDirectory, "{YOUR_BUCKET_NAME}");
});
```
Using in default mode:
```cs
// MyService.cs
public class MyService
{
    private readonly IStorage _storage;
  
    public MyService(IStorage storage)
    {
        _storage = storage;
    }
}
```
Provider-specific mode connection:
```cs
// Startup.cs
services.AddFileSystemStorage(new FileSystemStorageOptions
{
    BaseFolder = Path.Combine(Environment.CurrentDirectory, "{YOUR_BUCKET_NAME}"),
});
```
Using in provider-specific mode
```cs
// MyService.cs
public class MyService
{
    private readonly IFileSystemStorage _fileSystemStorage;
    public MyService(IFileSystemStorage fileSystemStorage)
    {
        _fileSystemStorage = fileSystemStorage;
    }
}
```
</details>

## How to use
We assume that below code snippets are placed in your service class with injected IStorage:

```cs
public class MyService
{
    private readonly IStorage _storage;
    public MyService(IStorage storage)
    {
        _storage = storage;
    }
}
```

### Upload
```cs
await _storage.UploadAsync(new Stream());
await _storage.UploadAsync("some string content");
await _storage.UploadAsync(new FileInfo("D:\\my_report.txt"));
```

### Delete
```cs
await _storage.DeleteAsync("my_report.txt");
```

### Download
```cs
var localFile = await _storage.DownloadAsync("my_report.txt");
```

### Get metadata
```cs
await _storage.GetBlobMetadataAsync("my_report.txt");
```


