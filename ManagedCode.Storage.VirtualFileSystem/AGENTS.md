Project: ManagedCode.Storage.VirtualFileSystem
Owned by: ManagedCode.Storage maintainers

Parent: `../AGENTS.md`

## Purpose

- Implements the virtual file system abstraction that layers directory and file semantics over `IStorage` providers.
- Exists to give consumers a higher-level file system API without coupling them to any single provider implementation.

## Entry Points

- `Core/IVirtualFileSystem.cs`
- `Implementations/VirtualFileSystem.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Metadata/IMetadataManager.cs`
- `Streaming/VfsWriteStream.cs`

## Boundaries

- In scope: virtual file and directory contracts, metadata handling, streaming, and DI wiring for VFS consumers
- Out of scope: provider SDK specifics, HTTP or SignalR transports, and Orleans persistence behavior
- Protected or high-risk areas: path semantics, metadata consistency, and any flow that resolves storages through `IStorageFactory`

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.VirtualFileSystem.csproj`
- `test`: `dotnet test ../Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release`
- `format`: `dotnet format ../ManagedCode.Storage.slnx`
- Active test framework: `xUnit`
- Runner model: `VSTest`
- Analyzer severity lives in the repo-root `.editorconfig`.

## Applicable Skills

- `mcaf-dotnet`
- `mcaf-testing`
- `mcaf-dotnet-xunit`
- `mcaf-dotnet-quality-ci`
- `mcaf-solid-maintainability`
- `mcaf-dotnet-features`

## Local Constraints

- Stricter maintainability limits: none; inherit the root defaults.
- Required local docs: `docs/Architecture.md`, `README.md`, and the nearest feature or ADR docs when public behavior changes.
- Local exception policy: inherit the root `exception_policy` and document any project-specific exception in the nearest ADR, feature doc, or local `AGENTS.md`.

## Local Rules

- Keep VFS layered on `IStorage` and `IStorageFactory`; do not add direct dependencies on provider internals.
- Preserve the separation between metadata management and byte-stream operations.
- Any keyed-storage behavior here must stay compatible with the StorageFactory conventions used across the repo.
