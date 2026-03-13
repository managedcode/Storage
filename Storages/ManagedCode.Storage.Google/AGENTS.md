Project: ManagedCode.Storage.Google
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Google Cloud Storage provider package that adapts bucket and object operations to `IStorage`.
- Exists to isolate GCS-specific options, credentials, and client wiring from the shared storage contracts.

## Entry Points

- `GCPStorage.cs`
- `GCPStorageProvider.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `IGCPStorage.cs`
- `Options/`

## Boundaries

- In scope: provider implementation, provider-specific options, DI extensions, and any client-wrapper code owned by this package
- Out of scope: shared storage contracts, ASP.NET or SignalR transports, Orleans persistence, and test-only fakes
- Protected or high-risk areas: bucket or object naming, credential handling, and DI registration symmetry

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.Google.csproj`
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
- Preserve GCS bucket and object semantics plus credential-driven client creation without leaking Google SDK details across boundaries.
