# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

### Basic Commands
- **Build**: `dotnet build` - Builds the entire solution
- **Restore**: `dotnet restore` - Restores NuGet packages
- **Test**: `dotnet test` - Runs all tests
- **Test with Coverage**: `dotnet test /p:CollectCoverage=true /p:CoverletOutput=coverage /p:CoverletOutputFormat=opencover`

### Testing Specific Projects
- **Run single test project**: `dotnet test Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj`
- **Run specific test**: `dotnet test --filter "ClassName.MethodName"`

### Project Structure
This is a .NET 9 solution targeting multiple cloud storage providers with a universal interface.

## Architecture Overview

### Core Architecture
The solution follows a provider pattern with:

- **Core Library** (`ManagedCode.Storage.Core`): Base interfaces and abstract classes
  - `IStorage<T, TOptions>`: Generic storage interface with client and options
  - `BaseStorage<T, TOptions>`: Abstract base implementation
  - Common models: `BlobMetadata`, `UploadOptions`, `DownloadOptions`, etc.

- **Storage Providers** (in `Storages/` directory):
  - `ManagedCode.Storage.Azure`: Azure Blob Storage
  - `ManagedCode.Storage.Azure.DataLake`: Azure Data Lake Storage
  - `ManagedCode.Storage.Aws`: Amazon S3
  - `ManagedCode.Storage.Google`: Google Cloud Storage
  - `ManagedCode.Storage.FileSystem`: Local file system

- **Integrations** (in `Integraions/` directory):
  - `ManagedCode.Storage.Server`: ASP.NET Core extensions
  - `ManagedCode.Storage.Client`: Client SDK
  - `ManagedCode.Storage.Client.SignalR`: SignalR integration

### Key Interfaces
- `IStorage`: Main storage interface combining uploader, downloader, streamer, and operations
- `IUploader`: File upload operations  
- `IDownloader`: File download operations
- `IStreamer`: Stream-based operations
- `IStorageOperations`: Blob metadata and existence operations

### Connection Modes
The library supports two connection modes:
1. **Default mode**: Use `IStorage` interface (single provider)
2. **Provider-specific mode**: Use provider-specific interfaces like `IAzureStorage`, `IAWSStorage`

### Provider Factory Pattern
- `IStorageFactory`: Creates storage instances
- `IStorageProvider`: Provider registration interface
- Extension methods for DI registration (e.g., `AddAzureStorage`, `AddAWSStorageAsDefault`)

## Testing
- Uses xUnit with FluentAssertions
- Testcontainers for integration testing (Azurite, LocalStack, FakeGcsServer)
- Test projects follow pattern: `Tests/ManagedCode.Storage.Tests/`
- Includes test fakes in `ManagedCode.Storage.TestFakes`

## Development Patterns
- All providers inherit from `BaseStorage<T, TOptions>`
- Options classes implement `IStorageOptions`
- Result pattern using `ManagedCode.Communication.Result<T>`
- Async/await throughout with CancellationToken support
- Dependency injection via extension methods

## Common Operations
```csharp
// Upload
await storage.UploadAsync(stream, options => {
    options.FileName = "file.txt";
    options.MimeType = "text/plain";
});

// Download  
var file = await storage.DownloadAsync("file.txt");

// Delete
await storage.DeleteAsync("file.txt");

// Check existence
var exists = await storage.ExistsAsync("file.txt");
```