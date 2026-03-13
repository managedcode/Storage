Project: ManagedCode.Storage.GoogleDrive
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Google Drive provider package that adapts Drive file storage to `IStorage`.
- Exists to keep Drive-specific client and auth concerns outside the shared abstractions.

## Entry Points

- `GoogleDriveStorage.cs`
- `GoogleDriveStorageProvider.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `IGoogleDriveStorage.cs`
- `Options/`

## Boundaries

- In scope: provider implementation, provider-specific options, DI extensions, and any client-wrapper code owned by this package
- Out of scope: shared storage contracts, ASP.NET or SignalR transports, Orleans persistence, and test-only fakes
- Protected or high-risk areas: Drive-specific client or auth boundaries, path mapping, and DI registration symmetry

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.GoogleDrive.csproj`
- `test`: `dotnet test ../../Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release`
- `format`: `dotnet format ../../ManagedCode.Storage.slnx`
- Active test framework: `xUnit`
- Runner model: `VSTest`
- Analyzer severity lives in the repo-root `.editorconfig`.

## Applicable Skills

- `mcaf-dotnet`
- `mcaf-testing`
- `mcaf-dotnet-xunit`
- `mcaf-dotnet-quality-ci`
- `mcaf-solid-maintainability`

## Local Constraints

- Stricter maintainability limits: none; inherit the root defaults.
- Required local docs: `docs/Architecture.md`, `README.md`, and the nearest feature or ADR docs when public behavior changes.
- Local exception policy: inherit the root `exception_policy` and document any project-specific exception in the nearest ADR, feature doc, or local `AGENTS.md`.

## Local Rules

- Keep default and keyed DI registrations aligned with StorageFactory conventions and the provider-specific interface exposure.
- Do not leak vendor SDK types outside provider-specific options, interfaces, or client wrappers.
- Keep Google Drive API types and auth details inside this provider boundary; only expose storage-shaped contracts outward.
