Project: ManagedCode.Storage.Client
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Provides the HTTP client surface that talks to the ASP.NET storage controllers.
- Exists so consumers can integrate with storage endpoints without depending on server-side implementation details.

## Entry Points

- `IStorageClient.cs`
- `StorageClient.cs`
- `ProgressStatus.cs`

## Boundaries

- In scope: HTTP request and response handling, client-facing transfer helpers, and public client contracts
- Out of scope: server routing policy, provider-specific behavior, and SignalR streaming internals
- Protected or high-risk areas: request or response shapes shared with `ManagedCode.Storage.Server`

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.Client.csproj`
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

- Keep the client contract aligned with `ManagedCode.Storage.Server/Controllers/IStorageController.cs`.
- Do not introduce provider-specific branches; this client stays storage-agnostic.
- Preserve stream-first transfer and cancellation behavior so large-file flows remain viable.
