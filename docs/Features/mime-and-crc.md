# Feature: MIME & Integrity Helpers (MimeHelper + CRC32)

## Purpose

Provide consistent content-type and integrity behaviour across providers and integrations:

- MIME type resolution via `MimeHelper`
- streamed CRC32 calculation via `Crc32Helper` for large-file validation and mirroring scenarios

## Main Flows

```mermaid
flowchart LR
  FileName[File name] --> Mime[MimeHelper.GetMimeType]
  Stream[Stream] --> CRC[Crc32Helper]
  CRC --> Validation[Compare checksums]
```

## Components

- MIME:
  - `ManagedCode.MimeTypes` (`MimeHelper`)
- CRC:
  - `ManagedCode.Storage.Core/Helpers/Crc32Helper.cs`
  - `ManagedCode.Storage.Core/Helpers/PathHelper.cs` (shared path utilities used by storages)

## Current Behavior

- MIME type is typically derived from the file name and stored as `BlobMetadata.MimeType` where providers support it.
- CRC32 can be computed without loading full content into memory (streamed processing).
- All MIME lookups should go through `MimeHelper` (avoid provider-specific or ad-hoc MIME detection).

## Tests

- `Tests/ManagedCode.Storage.Tests/Core/Crc32HelperTests.cs`

