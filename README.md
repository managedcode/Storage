![img|300x200](https://raw.githubusercontent.com/managedcode/Storage/main/logo.png)

# ManagedCode.Storage

[![CI](https://github.com/managedcode/Storage/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/managedcode/Storage/actions/workflows/ci.yml)
[![Release](https://github.com/managedcode/Storage/actions/workflows/release.yml/badge.svg?branch=main)](https://github.com/managedcode/Storage/actions/workflows/release.yml)
[![CodeQL](https://github.com/managedcode/Storage/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/Storage/actions/workflows/codeql-analysis.yml)
[![Codecov](https://codecov.io/gh/managedcode/Storage/graph/badge.svg?token=OMKP91GPVD)](https://codecov.io/gh/managedcode/Storage)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=managedcode_Storage&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=managedcode_Storage)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=managedcode_Storage&metric=coverage)](https://sonarcloud.io/summary/new_code?id=managedcode_Storage)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Core.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Core)

Cross-provider blob storage toolkit for .NET and ASP.NET streaming scenarios.

ManagedCode.Storage wraps vendor SDKs behind a single `IStorage` abstraction so uploads, downloads, metadata, streaming, and retention behave the same regardless of provider. Swap between Azure Blob Storage, Azure Data Lake, Amazon S3, Google Cloud Storage, OneDrive, Google Drive, Dropbox, CloudKit (iCloud app data), SFTP, a local file system, or the in-memory Virtual File System without rewriting application code. Pair it with our ASP.NET controllers and SignalR client to deliver chunked uploads, ranged downloads, and progress notifications end to end.

## Motivation

Cloud storage vendors expose distinct SDKs, option models, and authentication patterns. That makes it painful to change providers, run multi-region replication, or stand up hermetic tests. ManagedCode.Storage gives you a universal surface, consistent `Result<T>` handling, and DI-aware registration helpers so you can plug in any provider, test locally, and keep the same code paths in production.

## Features

- Unified `IStorage` abstraction covering upload, download, streaming, metadata, deletion, container management, and legal hold operations backed by `Result<T>` responses.
- Provider coverage across Azure Blob Storage, Azure Data Lake, Amazon S3, Google Cloud Storage, OneDrive (Microsoft Graph), Google Drive, Dropbox, CloudKit (iCloud app data), SFTP, local file system, and the in-memory Virtual File System (VFS).
- Keyed dependency-injection registrations plus default provider helpers to fan out files per tenant, region, or workload without manual service plumbing.
- ASP.NET storage controllers, chunk orchestration services, and a SignalR hub/client pair that deliver resumable uploads, ranged downloads, CRC32 validation, and real-time progress.
- `ManagedCode.Storage.Client` brings streaming uploads/downloads, CRC32 helpers, and MIME discovery via `MimeHelper` to any .NET app.
- Strongly typed option objects (`UploadOptions`, `DownloadOptions`, `DeleteOptions`, `MetadataOptions`, `LegalHoldOptions`, etc.) let you configure directories, metadata, and legal holds in one place.
- Virtual File System package keeps everything in memory for lightning-fast tests, developer sandboxes, and local demos while still exercising the same abstractions.
- Comprehensive automated test suite with cross-provider sync fixtures, multi-gigabyte streaming simulations (4 MB units per "GB"), ASP.NET controller harnesses, and SFTP/local filesystem coverage.
- ManagedCode.Storage.TestFakes package plus Testcontainers-based fixtures make it easy to run offline or CI tests without touching real cloud accounts.

## Packages

### Core & Utilities

| Package | Latest | Description |
| --- | --- | --- |
| [ManagedCode.Storage.Core](https://www.nuget.org/packages/ManagedCode.Storage.Core) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Core.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Core) | Core abstractions, option models, CRC32/MIME helpers, and DI extensions. |
| [ManagedCode.Storage.VirtualFileSystem](https://www.nuget.org/packages/ManagedCode.Storage.VirtualFileSystem) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.VirtualFileSystem.svg)](https://www.nuget.org/packages/ManagedCode.Storage.VirtualFileSystem) | In-memory storage built on the `IStorage` surface for tests and sandboxes. |
| [ManagedCode.Storage.TestFakes](https://www.nuget.org/packages/ManagedCode.Storage.TestFakes) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.TestFakes.svg)](https://www.nuget.org/packages/ManagedCode.Storage.TestFakes) | Provider doubles for unit/integration tests without hitting cloud services. |

### Providers

| Package | Latest | Description |
| --- | --- | --- |
| [ManagedCode.Storage.Azure](https://www.nuget.org/packages/ManagedCode.Storage.Azure) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Azure.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Azure) | Azure Blob Storage implementation with metadata, streaming, and legal hold support. |
| [ManagedCode.Storage.Azure.DataLake](https://www.nuget.org/packages/ManagedCode.Storage.Azure.DataLake) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Azure.DataLake.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Azure.DataLake) | Azure Data Lake Gen2 provider on top of the unified abstraction. |
| [ManagedCode.Storage.Aws](https://www.nuget.org/packages/ManagedCode.Storage.Aws) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Aws.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Aws) | Amazon S3 provider with Object Lock and legal hold operations. |
| [ManagedCode.Storage.Gcp](https://www.nuget.org/packages/ManagedCode.Storage.Gcp) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Gcp.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Gcp) | Google Cloud Storage integration built on official SDKs. |
| [ManagedCode.Storage.FileSystem](https://www.nuget.org/packages/ManagedCode.Storage.FileSystem) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.FileSystem.svg)](https://www.nuget.org/packages/ManagedCode.Storage.FileSystem) | Local file system implementation for hybrid or on-premises workloads. |
| [ManagedCode.Storage.Sftp](https://www.nuget.org/packages/ManagedCode.Storage.Sftp) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Sftp.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Sftp) | SFTP provider powered by SSH.NET for legacy and air-gapped environments. |
| [ManagedCode.Storage.OneDrive](https://www.nuget.org/packages/ManagedCode.Storage.OneDrive) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.OneDrive.svg)](https://www.nuget.org/packages/ManagedCode.Storage.OneDrive) | OneDrive provider built on Microsoft Graph. |
| [ManagedCode.Storage.GoogleDrive](https://www.nuget.org/packages/ManagedCode.Storage.GoogleDrive) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.GoogleDrive.svg)](https://www.nuget.org/packages/ManagedCode.Storage.GoogleDrive) | Google Drive provider built on the Google Drive API. |
| [ManagedCode.Storage.Dropbox](https://www.nuget.org/packages/ManagedCode.Storage.Dropbox) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Dropbox.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Dropbox) | Dropbox provider built on the Dropbox API. |
| [ManagedCode.Storage.CloudKit](https://www.nuget.org/packages/ManagedCode.Storage.CloudKit) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.CloudKit.svg)](https://www.nuget.org/packages/ManagedCode.Storage.CloudKit) | CloudKit (iCloud app data) provider built on CloudKit Web Services. |

### Configuring OneDrive, Google Drive, Dropbox, and CloudKit

> iCloud Drive does not expose a public server-side file API. `ManagedCode.Storage.CloudKit` targets CloudKit Web Services (iCloud app data), not iCloud Drive.

These providers follow the same DI patterns as the other backends: use `Add*StorageAsDefault(...)` to bind `IStorage`, or `Add*Storage(...)` to inject the provider interface (`IOneDriveStorage`, `IGoogleDriveStorage`, `IDropboxStorage`, `ICloudKitStorage`).

Most cloud-drive providers expect you to create the official SDK client (Graph/Drive/Dropbox) with your preferred auth flow and pass it into the storage options. ManagedCode.Storage does not run OAuth flows automatically.

Keyed registrations are available as well (useful for multi-tenant apps):

```csharp
using Dropbox.Api;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Dropbox.Extensions;

var accessToken = configuration["Dropbox:AccessToken"]; // obtained via OAuth (see Dropbox section below)
var dropboxClient = new DropboxClient(accessToken);

builder.Services.AddDropboxStorageAsDefault("tenant-a", options =>
{
    options.DropboxClient = dropboxClient;
    options.RootPath = "/apps/my-app";
});

var tenantStorage = app.Services.GetRequiredKeyedService<IStorage>("tenant-a");
```

**OneDrive / Microsoft Graph**

0. Install the provider package and import DI extensions:

   ```bash
   dotnet add package ManagedCode.Storage.OneDrive
   dotnet add package Azure.Identity
   ```

   ```csharp
   using ManagedCode.Storage.OneDrive.Extensions;
   ```

   Docs: [Register an app](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app), [Microsoft Graph auth](https://learn.microsoft.com/en-us/graph/auth/).

1. Create an app registration in Azure Active Directory (Entra ID) and record the **Application (client) ID**, **Directory (tenant) ID**, and a **client secret**.
2. In **API permissions**, add Microsoft Graph permissions:
   - For server-to-server apps: **Application** → `Files.ReadWrite.All` (or `Sites.ReadWrite.All` for SharePoint drives), then **Grant admin consent**.
   - For user flows: **Delegated** permissions are also possible, but you must supply a Graph client that authenticates as the user.
3. Create the Graph client (example uses client credentials):

   ```csharp
   using Azure.Identity;
   using Microsoft.Graph;

   var tenantId = configuration["OneDrive:TenantId"]!;
   var clientId = configuration["OneDrive:ClientId"]!;
   var clientSecret = configuration["OneDrive:ClientSecret"]!;

   var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
   var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
   ```

4. Register OneDrive storage with the Graph client and the drive/root you want to scope to:

   ```csharp
   builder.Services.AddOneDriveStorageAsDefault(options =>
   {
       options.GraphClient = graphClient;
       options.DriveId = "me";                   // or a specific drive ID
       options.RootPath = "app-data";            // folder will be created when CreateContainerIfNotExists is true
       options.CreateContainerIfNotExists = true;
   });
   ```

5. If you need a concrete drive id, fetch it via Graph (example):

   ```csharp
   var drive = await graphClient.Me.Drive.GetAsync();
   var driveId = drive?.Id;
   ```

**Google Drive**

0. Install the provider package and import DI extensions:

   ```bash
   dotnet add package ManagedCode.Storage.GoogleDrive
   ```

   ```csharp
   using ManagedCode.Storage.GoogleDrive.Extensions;
   ```

   Docs: [Drive API overview](https://developers.google.com/drive/api/guides/about-sdk), [OAuth 2.0](https://developers.google.com/identity/protocols/oauth2).

1. In [Google Cloud Console](https://console.cloud.google.com/), create a project and enable the **Google Drive API**.
2. Create credentials:
   - **Service account** (recommended for server apps): create a service account and download a JSON key.
   - **OAuth client** (interactive user auth): configure OAuth consent screen and create an OAuth client id/secret.
3. Create a `DriveService`.

   **Service account example:**

   ```csharp
   using Google.Apis.Auth.OAuth2;
   using Google.Apis.Drive.v3;
   using Google.Apis.Services;

   var credential = GoogleCredential
       .FromFile("service-account.json")
       .CreateScoped(DriveService.Scope.Drive);

   var driveService = new DriveService(new BaseClientService.Initializer
   {
       HttpClientInitializer = credential,
       ApplicationName = "MyApp"
   });
   ```

   If you use a service account, share the target folder/drive with the service account email (or use a Shared Drive) so it can see your files.

4. Register the Google Drive provider with the configured `DriveService` and a root folder id:

   ```csharp
   builder.Services.AddGoogleDriveStorageAsDefault(options =>
   {
       options.DriveService = driveService;
       options.RootFolderId = "root"; // or a specific folder id you control
       options.CreateContainerIfNotExists = true;
   });
   ```

5. Store tokens in user secrets or environment variables; never commit them to source control.

**Dropbox**

0. Install the provider package and import DI extensions:

   ```bash
   dotnet add package ManagedCode.Storage.Dropbox
   ```

   ```csharp
   using ManagedCode.Storage.Dropbox.Extensions;
   ```

   Docs: [Dropbox App Console](https://www.dropbox.com/developers/apps), [OAuth guide](https://www.dropbox.com/developers/documentation/http/documentation#oauth2).

1. Create an app in the [Dropbox App Console](https://www.dropbox.com/developers/apps) and choose **Scoped access** with the **Full Dropbox** or **App folder** type.
2. Record the **App key** and **App secret** (Settings tab).
3. Under **Permissions**, enable `files.content.write`, `files.content.read`, `files.metadata.read`, and `files.metadata.write` (plus any additional scopes you need) and save changes.
4. Obtain an access token:
   - For quick local testing, you can generate a token in the app console.
   - For production, use OAuth code flow (example):

   ```csharp
   using Dropbox.Api;

   var appKey = configuration["Dropbox:AppKey"]!;
   var appSecret = configuration["Dropbox:AppSecret"]!;
   var redirectUri = configuration["Dropbox:RedirectUri"]!; // must be registered in Dropbox app console

   // 1) Redirect user to:
   // var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, appKey, redirectUri, tokenAccessType: TokenAccessType.Offline);
   //
   // 2) Receive the 'code' on your redirect endpoint, then exchange it:
   var auth = await DropboxOAuth2Helper.ProcessCodeFlowAsync(code, appKey, appSecret, redirectUri);
   var accessToken = auth.AccessToken;
   var refreshToken = auth.RefreshToken; // store securely if you requested offline access
   ```

5. Create the Dropbox client and register Dropbox storage with a root path (use `/` for full access apps or `/Apps/<your-app>` for app folders):

   ```csharp
   using Dropbox.Api;
   builder.Services.AddDropboxStorageAsDefault(options =>
   {
       var accessToken = configuration["Dropbox:AccessToken"]!;
       options.DropboxClient = new DropboxClient(accessToken);
       options.RootPath = "/apps/my-app";
       options.CreateContainerIfNotExists = true;
   });
   ```

6. Store tokens in user secrets or environment variables; never commit them to source control.

**CloudKit (iCloud app data)**

0. Install the provider package and import DI extensions:

   ```bash
   dotnet add package ManagedCode.Storage.CloudKit
   ```

   ```csharp
   using ManagedCode.Storage.CloudKit.Extensions;
   using ManagedCode.Storage.CloudKit.Options;
   ```

   Docs: [CloudKit Web Services Reference](https://developer.apple.com/library/archive/documentation/DataManagement/Conceptual/CloudKitWebServicesReference/index.html).

1. In Apple Developer / CloudKit Dashboard, configure the container you want to use and note its container id (example: `iCloud.com.company.app`).
2. Ensure the file record type exists (default `MCStorageFile`).
3. Add these fields to the record type:
   - `path` (String) — must be queryable/indexed for prefix listing.
   - `contentType` (String) — optional but recommended.
   - `file` (Asset) — stores the binary content.
4. Configure authentication:
   - **API token** (`ckAPIToken`): create an API token for your container in CloudKit Dashboard and store it as a secret.
   - **Server-to-server key** (public DB only): create a CloudKit key in Apple Developer (download the `.p8` private key, keep the key id).
5. Register CloudKit storage:

   ```csharp
   builder.Services.AddCloudKitStorageAsDefault(options =>
   {
       options.ContainerId = "iCloud.com.company.app";
       options.Environment = CloudKitEnvironment.Production;
       options.Database = CloudKitDatabase.Public;
       options.RootPath = "app-data";

       // Choose ONE auth mode:
       options.ApiToken = configuration["CloudKit:ApiToken"];
       // OR:
       // options.ServerToServerKeyId = configuration["CloudKit:KeyId"];
       // options.ServerToServerPrivateKeyPem = configuration["CloudKit:PrivateKeyPem"]; // paste PEM (.p8) contents
   });
   ```

6. CloudKit Web Services impose size limits; keep files reasonably small and validate against your current CloudKit quotas.

### ASP.NET & Clients

| Package | Latest | Description |
| --- | --- | --- |
| [ManagedCode.Storage.Server](https://www.nuget.org/packages/ManagedCode.Storage.Server) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Server.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Server) | ASP.NET controllers, chunk orchestration services, and the SignalR storage hub. |
| [ManagedCode.Storage.Client](https://www.nuget.org/packages/ManagedCode.Storage.Client) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Client.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Client) | .NET client SDK for uploads, downloads, metadata, and SignalR negotiations. |
| [ManagedCode.Storage.Client.SignalR](https://www.nuget.org/packages/ManagedCode.Storage.Client.SignalR) | [![NuGet](https://img.shields.io/nuget/v/ManagedCode.Storage.Client.SignalR.svg)](https://www.nuget.org/packages/ManagedCode.Storage.Client.SignalR) | SignalR streaming client for browsers and native applications. |

## Architecture

### Storage Topology

The topology below shows how applications talk to the shared `IStorage` surface, optional Virtual File System, and keyed provider factories before landing on the concrete backends.

```mermaid
flowchart LR
    subgraph Applications
        API["ASP.NET Controllers"]
        SignalRClient["SignalR Client"]
        Workers["Background Services"]
    end

    subgraph Abstraction
        Core["IStorage Abstractions"]
        VFS["Virtual File System"]
        Factories["Keyed Provider Factories"]
    end

    subgraph Providers
        Azure["Azure Blob"]
        AzureDL["Azure Data Lake"]
        Aws["Amazon S3"]
        Gcp["Google Cloud Storage"]
        OneDrive["OneDrive (Graph)"]
        GoogleDrive["Google Drive"]
        Dropbox["Dropbox"]
        CloudKit["CloudKit (iCloud app data)"]
        Fs["File System"]
        Sftp["SFTP"]
    end

    Applications --> Core
    Core --> VFS
    Core --> Factories
    Factories --> Azure
    Factories --> AzureDL
    Factories --> Aws
    Factories --> Gcp
    Factories --> OneDrive
    Factories --> GoogleDrive
    Factories --> Dropbox
    Factories --> CloudKit
    Factories --> Fs
    Factories --> Sftp
```

Keyed provider registrations let you resolve multiple named instances from dependency injection while reusing the same abstraction across Azure, AWS, Google Cloud Storage, Google Drive, OneDrive, Dropbox, CloudKit, SFTP, and local file system storage.

### ASP.NET Streaming Controllers

Controllers in `ManagedCode.Storage.Server` expose minimal routes that stream directly between HTTP clients and blob providers. Uploads arrive as multipart forms or raw streams, flow through the unified `IStorage` abstraction, and land in whichever provider is registered. Downloads return `FileStreamResult` responses so browsers, SDKs, or background jobs can read blobs without buffering the whole payload in memory.

```mermaid
sequenceDiagram
    participant Client as Client App
    participant Controller as StorageController
    participant Storage as IStorage
    participant Provider as IStorage Provider

    Client->>Controller: POST /storage/upload (stream)
    Controller->>Storage: UploadAsync(stream, UploadOptions)
    Storage->>Provider: Push stream to backend
    Provider-->>Storage: Result<BlobMetadata>
    Storage-->>Controller: Upload response
    Controller-->>Client: 200 OK + metadata

    Client->>Controller: GET /storage/download?file=video.mp4
    Controller->>Storage: DownloadAsync(file)
    Storage->>Provider: Open download stream
    Provider-->>Storage: Result<Stream>
    Storage-->>Controller: Stream payload
    Controller-->>Client: Chunked response
```

Controllers remain thin: consumers can inherit and override actions to add custom routing, authorization, or telemetry while leaving the streaming plumbing intact.

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

## ASP.NET Controllers & Streaming

The <code>ManagedCode.Storage.Server</code> package surfaces upload/download controllers that pipe HTTP streams straight into the storage abstraction. Files can be sent as multipart forms or raw streams, while downloads return <code>FileStreamResult</code> so large assets flow back to the caller without buffering in memory.

```csharp
// Program.cs / Startup.cs
builder.Services.AddStorageServer(options =>
{
    options.EnableRangeProcessing = true;              // support range/seek operations
    options.InMemoryUploadThresholdBytes = 512 * 1024;  // spill to disk after 512 KB
});

app.MapControllers(); // exposes /storage endpoints
```

When you need custom routes, validation, or policies, inherit from the base controller and reuse the same streaming helpers:

```csharp
[Route("api/files")]
public sealed class FilesController : StorageControllerBase<IMyCustomStorage>
{
    public FilesController(
        IMyCustomStorage storage,
        ChunkUploadService chunks,
        StorageServerOptions options)
        : base(storage, chunks, options)
    {
    }
}

// Upload a form file directly into storage
public Task<IActionResult> Upload(IFormFile file, CancellationToken ct) =>
    UploadFormFileAsync(file, ct);

// Stream a blob to the client in real time
public Task<IActionResult> Download(string fileName, CancellationToken ct) =>
    DownloadAsStreamAsync(fileName, ct);
```

Need resumable uploads or live progress UI? Call <code>AddStorageSignalR()</code> to enable the optional hub and connect with the <code>ManagedCode.Storage.Client.SignalR</code> package; otherwise, the controllers alone cover straight HTTP streaming scenarios.

## Connection modes

You can connect storage interface in two modes provider-specific and default. In case of default you are restricted with
one storage type

Cloud-drive providers (OneDrive, Google Drive, Dropbox) and CloudKit are configured in [Configuring OneDrive, Google Drive, Dropbox, and CloudKit](#configuring-onedrive-google-drive-dropbox-and-cloudkit); the same default/provider-specific rules apply.

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
