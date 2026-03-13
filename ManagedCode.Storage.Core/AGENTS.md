Project: ManagedCode.Storage.Core
Owned by: ManagedCode.Storage maintainers

Parent: `../AGENTS.md`

## Purpose

- Defines the shared storage contracts, options, helpers, exceptions, and factory abstractions used by every other package.
- Exists to keep vendor-specific providers and delivery packages isolated behind stable `IStorage`-based interfaces.

## Entry Points

- `IStorage.cs`
- `Providers/IStorageFactory.cs`
- `Providers/IStorageProvider.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `BaseStorage.cs`

## Boundaries

- In scope: public storage abstractions, shared models or helpers, and DI bootstrap for the storage factory
- Out of scope: vendor SDK logic, HTTP or SignalR transports, Orleans glue, VFS-specific behavior, and test-only fakes
- Protected or high-risk areas: `IStorage` contract shape, keyed/default factory resolution, and exceptions consumed by downstream packages

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.Core.csproj`
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

- Treat `IStorage`, `IStorageProvider`, and factory abstractions as public contracts; changing them is an ask-first API change.
- Keep shared helpers vendor-agnostic; SDK-specific behavior belongs in provider projects, not in Core.
- Preserve keyed and default storage resolution semantics because VFS, server integrations, and Orleans depend on them.
