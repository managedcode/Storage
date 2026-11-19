---
keywords: "IStorage, ManagedCode.Storage.Core, provider-agnostic storage, upload, download, streaming, metadata, Result<T>, .NET"
---

# Feature: Storage Core Abstraction (`ManagedCode.Storage.Core`)

## Purpose

Provide a single, provider-agnostic storage API for .NET so application code can upload/download/list/stream files without being coupled to vendor SDKs.

The core package defines:

- `IStorage` and related capability interfaces (`IUploader`, `IDownloader`, `IStreamer`)
- consistent `Result` / `Result<T>` error handling
- common option models (upload/download/delete/metadata/legal hold)
- provider factories (`IStorageProvider`, `IStorageFactory`) and DI helpers

## Main Flows

### 1) Upload / Download

```mermaid
flowchart LR
  App[Application code] --> I[IStorage]
  I --> B[BaseStorage<TClient, TOptions>]
  B --> C[Provider storage client]
  C --> SDK[Vendor SDK / API]
  SDK --> B
  B --> I
  I --> App
```

### 2) Options-driven pathing + metadata

- Callers pass `UploadOptions` / `DownloadOptions` / `MetadataOptions` to control:
  - `FileName`, `Directory` (logical path)
  - `MimeType` (normalized through `MimeHelper`)
  - per-provider metadata where supported

### 3) Copy and move between storages

`CopyToAsync` and `MoveToAsync` in `ManagedCode.Storage.Core.Extensions` take a source
storage and path, a destination storage and path, and an optional cancellation token.
The storages can use different providers. Each successful call returns the destination's
`BlobMetadata`.

```mermaid
flowchart LR
  Source["Read source metadata"] --> Check["Check that destination is vacant"]
  Check --> Upload["Upload content and verify length"]
  Upload --> Done["Return destination metadata for copy"]
  Upload --> Delete["Delete source for move"]
  Delete --> DoneMove["Return destination metadata after deletion"]
```

- If the destination already contains a file at the given path, the operation returns a `409` conflict.
- Content type and custom metadata travel through the destination provider's upload options. What persists depends on the provider.
- Seekable sources stream directly to the destination. Sequential sources use a temporary local file that the transfer disposes, so providers can determine the length and rewind during upload.
- Coordinate access so other writers and deleters cannot modify either path during the operation. The existence check, upload, and delete are separate provider calls.
- A failed or cancelled write preserves the source. A provider can leave data at the destination if its upload fails after writing. If source deletion fails after a completed upload, the destination stays available. Before retrying a partial move, verify both paths and reconcile the existing destination.

## Components

Key files:

- `ManagedCode.Storage.Core/IStorage.cs` — main abstraction surface
- `ManagedCode.Storage.Core/BaseStorage.cs` — shared lifecycle + template methods for providers
- `ManagedCode.Storage.Core/Models/*.cs` — option models and `BlobMetadata` / `LocalFile`
- `ManagedCode.Storage.Core/Helpers/Crc32Helper.cs` — streamed CRC32 helpers used by chunked uploads/downloads
- `ManagedCode.Storage.Core/Helpers/PathHelper.cs` — shared path manipulation utilities
- `ManagedCode.Storage.Core/Providers/*` — provider factory abstractions and the default `StorageFactory`
- `ManagedCode.Storage.Core/Extensions/ServiceCollectionExtensions.cs` — DI helpers
- [`StorageTransferExtensions`](https://github.com/managed-code-hub/Storage/blob/main/ManagedCode.Storage.Core/Extensions/StorageTransferExtensions.cs) — cross-storage file copy and move

## Current Behavior

- Provider implementations typically inherit `BaseStorage<TClient, TOptions>` and only implement provider-specific operations.
- Public operations return `Result`/`Result<T>` with:
  - `IsSuccess` and (when successful) `Value`
  - exceptions captured and returned as failed results
- MIME type lookups should flow through `MimeHelper` (do not add ad-hoc MIME detection).
- `StringStream` exposes the original UTF-16 code units as little-endian bytes. Byte positions can be odd or past the end of the stream. Attempts to move before the beginning fail and preserve the current position.
- `VerifiedContentSnapshot` checks the declared length and SHA-256 before returning a file-backed snapshot. It reads at most one byte beyond the declared length to detect trailing content.
- `LocalFile.FromStreamAsync` copies from a caller-owned stream and replaces the destination contents. On failure it closes the destination and removes a newly created file. Copies to an existing named destination write in place.
- Downloads release their temporary destination when the provider reports failure or returns a file at a different path. A caller-supplied destination stays on disk.

## Tests

- `Tests/ManagedCode.Storage.Tests/Core/Crc32HelperTests.cs`
- `Tests/ManagedCode.Storage.Tests/Core/LocalFileTests.cs`
- `Tests/ManagedCode.Storage.Tests/Core/StringStreamTests.cs`
- `Tests/ManagedCode.Storage.Tests/Core/StorageClientChunkTests.cs` (core chunk behaviour used by clients)

## References

- `README.md` (package list + quickstart)
- `docs/Testing/strategy.md` (suite structure and rules)
