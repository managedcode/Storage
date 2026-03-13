Project: ManagedCode.Storage.Orleans
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Provides an Orleans `IGrainStorage` implementation backed by ManagedCode.Storage abstractions.
- Exists so Orleans persistent state can reuse any registered ManagedCode storage provider through default, typed, or keyed DI resolution.

## Entry Points

- `Hosting/ManagedCodeStorageGrainStorageServiceCollectionExtensions.cs`
- `Hosting/ManagedCodeStorageGrainStorageSiloBuilderExtensions.cs`
- `Storage/ManagedCodeGrainStorage.cs`
- `Configuration/ManagedCodeStorageGrainStorageOptions.cs`

## Boundaries

- In scope: Orleans provider registration, storage-resolution options, grain-state serialization, and `IGrainStorage` semantics
- Out of scope: vendor-specific provider implementations, ASP.NET transport logic, and grain business logic
- Protected or high-risk areas: named/default provider registration, keyed storage lookup, and Orleans `ETag` or clear-state behavior

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.Orleans.csproj`
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
- `mcaf-dotnet-features`

## Local Constraints

- Stricter maintainability limits: none; inherit the root defaults.
- Required local docs: `docs/Architecture.md`, `README.md`, and the nearest feature or ADR docs when public behavior changes.
- Local exception policy: inherit the root `exception_policy` and document any project-specific exception in the nearest ADR, feature doc, or local `AGENTS.md`.

## Local Rules

- Keep Orleans named and default registrations aligned with ManagedCode Storage typed or keyed resolution rules.
- Preserve Orleans `ETag`, read, write, and clear semantics; stale-write behavior is part of the contract.
- Storage lookup must stay on ManagedCode abstractions, not vendor-specific clients or provider internals.
