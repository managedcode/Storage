![img|300x200](https://raw.githubusercontent.com/managedcode/Storage/main/logo.png)

# ManagedCode.Storage

[![.NET](https://github.com/managedcode/Storage/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/Storage/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/managedcode/Storage/graph/badge.svg?token=OMKP91GPVD)](https://codecov.io/gh/managedcode/Storage)
[![nuget](https://github.com/managedcode/Storage/actions/workflows/nuget.yml/badge.svg?branch=main)](https://github.com/managedcode/Storage/actions/workflows/nuget.yml)
[![CodeQL](https://github.com/managedcode/Storage/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/Storage/actions/workflows/codeql-analysis.yml)

[![Alert Status](https://sonarcloud.io/api/project_badges/measure?project=managedcode_Storage&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=managedcode_Storage)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=managedcode_Storage&metric=coverage)](https://sonarcloud.io/summary/new_code?id=managedcode_Storage)

| Version                                                                                                                                                          | Package                                                                                                     | Description      |
|------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------|------------------|
| [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Core.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Core)                         | [ManagedCode.Storage.Core](https://www.nuget.org/packages/ManagedCode.Storage.Core)                         | Core             |
| [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.FileSystem.svg)](https://www.nuget.org/packages/ManagedCode.Storage.FileSystem)             | [ManagedCode.Storage.FileSystem](https://www.nuget.org/packages/ManagedCode.Storage.FileSystem)             | FileSystem       |
| [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Azure.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Azure)                       | [ManagedCode.Storage.Azure](https://www.nuget.org/packages/ManagedCode.Storage.Azure)                       | Azure            |
| [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Aws.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Aws)                           | [ManagedCode.Storage.Aws](https://www.nuget.org/packages/ManagedCode.Storage.Aws)                           | AWS              |
| [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Gcp.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Gcp)                           | [ManagedCode.Storage.Gcp](https://www.nuget.org/packages/ManagedCode.Storage.Gcp)                           | GCP              |
| [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.AspNetExtensions.svg)](https://www.nuget.org/packages/ManagedCode.Storage.AspNetExtensions) | [ManagedCode.Storage.AspNetExtensions](https://www.nuget.org/packages/ManagedCode.Storage.AspNetExtensions) | AspNetExtensions |
| [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Server.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Server) | [ManagedCode.Storage.Server](https://www.nuget.org/packages/ManagedCode.Storage.Server) | ASP.NET Server |
| [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.Storage.Client.SignalR.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Client.SignalR) | [ManagedCode.Storage.Client.SignalR](https://www.nuget.org/packages/ManagedCode.Storage.Client.SignalR) | SignalR Client |

# Storage
---

## General concept

One of the key benefits of using a universal wrapper for cloud blob storages is that it provides a consistent,
easy-to-use interface for working with different types of blob storage. This can make it much easier for developers to
switch between different storage providers, or to use multiple providers in the same project.

A universal wrapper can also simplify the development process by providing a single set of methods for working with blob
storage, rather than requiring developers to learn and use the different APIs provided by each storage provider. This
can save time and reduce the complexity of the code, making it easier to write, maintain, and debug.

In addition, a universal wrapper can provide additional functionality that is not available through the individual
storage providers, such as support for common patterns like asynchronous programming and error handling. This can make
it easier to write high-quality, reliable code that is robust and resilient to errors.

Overall, using a universal wrapper for cloud blob storages can provide many benefits, including improved flexibility,
simplicity, and reliability in your application.
A universal storage for working with multiple storage providers:

- Azure
- Google Cloud
- Amazon
- FileSystem

## Motivation

Cloud storage is a popular and convenient way to store and access data in the cloud.
However, different cloud storage providers often have their own unique APIs and interfaces for accessing and
manipulating data.
This can make it difficult to switch between different providers or to use multiple providers simultaneously.

Our library, provides a universal interface for accessing and manipulating data in different cloud blob storage
providers.
This makes it easy to switch between providers or to use multiple providers at the same time, without having to learn
and use multiple APIs.

## Features

- Provides a universal interface for accessing and manipulating data in different cloud blob storage providers.
- Makes it easy to switch between providers or to use multiple providers simultaneously.
- Supports common operations such as uploading, downloading, and deleting data, plus optional in-memory Virtual File System (VFS) storage for fast testing.
- Provides first-class ASP.NET controller extensions and a SignalR hub/client pairing (two-step streaming handshake) for uploads, downloads, and chunk orchestration.
- Ships keyed dependency-injection helpers so you can register multiple named providers and mirror assets across regions or vendors.
- Exposes configurable server options for large-file thresholds, multipart parsing limits, and range streaming.

## Virtual File System (VFS)

Need to hydrate storage dependencies without touching disk or the cloud? The <code>ManagedCode.Storage.VirtualFileSystem</code> package keeps everything in memory and makes it trivial to stand up repeatable tests or developer sandboxes:

```csharp
// Program.cs / Startup.cs
builder.Services.AddVirtualFileSystemStorageAsDefault(options =>
{
    options.StorageName = "vfs";   // optional logical name
});

// Usage
public class MyService
{
    private readonly IStorage storage;

    public MyService(IStorage storage) => this.storage = storage;

    public Task UploadAsync(Stream stream, string path) => storage.UploadAsync(stream, new UploadOptions(path));
}

// In tests you can pre-populate the VFS
await storage.UploadAsync(new FileInfo("fixtures/avatar.png"), new UploadOptions("avatars/user-1.png"));
```

Because the VFS implements the same abstractions as every other provider, you can swap it for in-memory integration tests while hitting Azure, S3, etc. in production.

## Dependency Injection & Keyed Registrations

Every provider ships with default and provider-specific registrations, but you can also assign multiple named instances using .NET's keyed services. This makes it easy to route traffic to different containers/buckets (e.g. <code>azure-primary</code> vs. <code>azure-dr</code>) or to fan out a file to several backends:

```csharp
using Amazon;
using Amazon.S3;
using ManagedCode.MimeTypes;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

builder.Services
    .AddAzureStorage("azure-primary", options =>
    {
        options.ConnectionString = configuration["Storage:Azure:Primary:ConnectionString"]!;
        options.Container = "assets";
    })
    .AddAzureStorage("azure-dr", options =>
    {
        options.ConnectionString = configuration["Storage:Azure:Dr:ConnectionString"]!;
        options.Container = "assets-dr";
    })
    .AddAWSStorage("aws-backup", options =>
    {
        options.PublicKey = configuration["Storage:Aws:AccessKey"]!;
        options.SecretKey = configuration["Storage:Aws:SecretKey"]!;
        options.Bucket = "assets-backup";
        options.OriginalOptions = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.USEast1
        };
    });

public sealed class AssetReplicator
{
    private readonly IAzureStorage _primary;
    private readonly IAzureStorage _disasterRecovery;
    private readonly IAWSStorage _backup;

    public AssetReplicator(
        [FromKeyedServices("azure-primary")] IAzureStorage primary,
        [FromKeyedServices("azure-dr")] IAzureStorage secondary,
        [FromKeyedServices("aws-backup")] IAWSStorage backup)
    {
        _primary = primary;
        _disasterRecovery = secondary;
        _backup = backup;
    }

    public async Task MirrorAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        buffer.Position = 0;
        var uploadOptions = new UploadOptions(fileName, mimeType: MimeHelper.GetMimeType(fileName));

        await _primary.UploadAsync(buffer, uploadOptions, cancellationToken);

        buffer.Position = 0;
        await _disasterRecovery.UploadAsync(buffer, uploadOptions, cancellationToken);

        buffer.Position = 0;
        await _backup.UploadAsync(buffer, uploadOptions, cancellationToken);
    }
}
```

Keyed services can also be resolved via <code>IServiceProvider.GetRequiredKeyedService&lt;T&gt;("key")</code> when manual dispatching is required.

Want to double-check data fidelity after copying? Pair uploads with <code>Crc32Helper</code>:

```csharp
var download = await _backup.DownloadAsync(fileName, cancellationToken);
download.IsSuccess.ShouldBeTrue();

await using var local = download.Value;
var crc = Crc32Helper.CalculateFileCrc(local.FilePath);
logger.LogInformation("Backup CRC for {File} is {Crc}", fileName, crc);
```

The test suite includes end-to-end scenarios that mirror payloads between Azure, AWS, the local file system, and virtual file systems; multi-gigabyte flows execute by default across every provider using 4 MB units per "GB" to keep runs fast while still exercising streaming paths.

## ASP.NET Controllers & SignalR Streaming

The <code>ManagedCode.Storage.Server</code> package exposes ready-to-use controllers plus a SignalR hub that sit on top of any <code>IStorage</code> implementation.
Pair it with the <code>ManagedCode.Storage.Client.SignalR</code> library to stream files from browsers, desktop or mobile apps:

```csharp
// Program.cs / Startup.cs
builder.Services
    .AddStorageServer(options =>
    {
        options.InMemoryUploadThresholdBytes = 512 * 1024; // spill to disk after 512 KB
        options.MultipartBoundaryLengthLimit = 128;        // relax multipart parsing limit
    })
    .AddStorageSignalR();    // registers StorageHub options

app.MapControllers();
app.MapStorageHub();        // maps /hubs/storage by default

// Client usage
var client = new StorageSignalRClient(new StorageSignalRClientOptions
{
    HubUrl = new Uri("https://myapi/hubs/storage")
});

await client.ConnectAsync();
await client.UploadAsync(fileStream, new StorageUploadStreamDescriptor
{
    FileName = "video.mp4",
    ContentType = "video/mp4"
});

// Download back into a stream
await client.DownloadAsync("video.mp4", destinationStream);
```

Events such as <code>TransferProgress</code> and <code>TransferCompleted</code> fire automatically, enabling live progress UI or resumable workflows. Extending the default controller is a one-liner:

```csharp
[Route("api/files")]
public sealed class FilesController : StorageControllerBase<IMyCustomStorage>
{
    public FilesController(IMyCustomStorage storage,
        ChunkUploadService chunks,
        StorageServerOptions options)
        : base(storage, chunks, options)
    {
    }
}

// Program.cs
builder.Services.AddStorageServer(opts =>
{
    opts.EnableRangeProcessing = true;
    opts.InMemoryUploadThresholdBytes = 1 * 1024 * 1024; // 1 MB
});
builder.Services.AddStorageSignalR();

app.MapControllers();
app.MapStorageHub();
```

Use the built-in controller extension methods to tailor behaviours (e.g. <code>UploadFormFileAsync</code>, <code>DownloadAsStreamAsync</code>) or override the base actions to add authorization filters, custom routing, or domain-specific validation.

> SignalR uploads follow a two-phase handshake: the client calls <code>BeginUploadStreamAsync</code> to reserve an identifier, then streams payloads through <code>UploadStreamContentAsync</code> while consuming the server-generated status channel. The <code>StorageSignalRClient</code> handles this workflow automatically.

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

> Need multiple Azure accounts or containers? Call <code>services.AddAzureStorage("azure-primary", ...)</code> and decorate constructor parameters with <code>[FromKeyedServices("azure-primary")]</code>.

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

> Need parallel S3 buckets? Register them with <code>AddAWSStorage("aws-backup", ...)</code> and inject via <code>[FromKeyedServices("aws-backup")]</code>.

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
    private readonly IAWSStorage _storage;
    public MyService(IAWSStorage storage)
    {
        _storage = storage;
    }
}
```

> Need parallel S3 buckets? Register them with <code>AddAWSStorage("aws-backup", ...)</code> and inject via <code>[FromKeyedServices("aws-backup")]</code>.

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

> Mirror to multiple folders? Use <code>AddFileSystemStorage("archive", options => options.BaseFolder = ...)</code> and resolve them via <code>[FromKeyedServices("archive")]</code>.

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

In summary, Storage library provides a universal interface for accessing and manipulating data in different cloud blob
storage providers, plus ready-to-host ASP.NET controllers, SignalR streaming endpoints, keyed dependency injection, and
a memory-backed VFS.
It makes it easy to switch between providers or to use multiple providers simultaneously, without having to learn and
use multiple APIs, while staying in full control of routing, thresholds, and mirroring.
We hope you find it useful in your own projects!
