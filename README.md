![img|300x200](https://raw.githubusercontent.com/managedcode/Storage/main/logo.png)

# ManagedCode.Storage

[![.NET](https://github.com/managedcode/Storage/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/Storage/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/managedcode/Storage/graph/badge.svg?token=OMKP91GPVD)](https://codecov.io/gh/managedcode/Storage)
[![nuget](https://github.com/managedcode/Storage/actions/workflows/nuget.yml/badge.svg?branch=main)](https://github.com/managedcode/Storage/actions/workflows/nuget.yml)
[![CodeQL](https://github.com/managedcode/Storage/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/Storage/actions/workflows/codeql-analysis.yml)


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

## General concept
One of the key benefits of using a universal wrapper for cloud blob storages is that it provides a consistent, easy-to-use interface for working with different types of blob storage. This can make it much easier for developers to switch between different storage providers, or to use multiple providers in the same project.

A universal wrapper can also simplify the development process by providing a single set of methods for working with blob storage, rather than requiring developers to learn and use the different APIs provided by each storage provider. This can save time and reduce the complexity of the code, making it easier to write, maintain, and debug.

In addition, a universal wrapper can provide additional functionality that is not available through the individual storage providers, such as support for common patterns like asynchronous programming and error handling. This can make it easier to write high-quality, reliable code that is robust and resilient to errors.

Overall, using a universal wrapper for cloud blob storages can provide many benefits, including improved flexibility, simplicity, and reliability in your application.
A universal storage for working with multiple storage providers:
- Azure
- Google Cloud
- Amazon
- FileSystem

## Motivation
Cloud storage is a popular and convenient way to store and access data in the cloud. 
However, different cloud storage providers often have their own unique APIs and interfaces for accessing and manipulating data. 
This can make it difficult to switch between different providers or to use multiple providers simultaneously.

Our library, provides a universal interface for accessing and manipulating data in different cloud blob storage providers. 
This makes it easy to switch between providers or to use multiple providers at the same time, without having to learn and use multiple APIs.

## Features
- Provides a universal interface for accessing and manipulating data in different cloud blob storage providers.
- Makes it easy to switch between providers or to use multiple providers simultaneously.
- Supports common operations such as uploading, downloading, and deleting data.

## Connection modes

You can connect storage interface in two modes provider-specific and default. In case of default you are restricted with
one storage type

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

### Native client

If you need more flexibility, you can use native client for any IStorage&lt;T&gt;

```cs
_storage.StorageClient
```

## Conclusion
In summary, Storage library provides a universal interface for accessing and manipulating data in different cloud blob storage providers. 
It makes it easy to switch between providers or to use multiple providers simultaneously, without having to learn and use multiple APIs. 
We hope you find it useful in your own projects!
