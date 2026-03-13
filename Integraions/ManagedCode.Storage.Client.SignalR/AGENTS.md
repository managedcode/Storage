Project: ManagedCode.Storage.Client.SignalR
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Provides the SignalR client used for streaming transfers and progress notifications.
- Exists so callers can consume the server hub contract without referencing the ASP.NET host project directly.

## Entry Points

- `IStorageSignalRClient.cs`
- `StorageSignalRClient.cs`
- `StorageSignalRClientOptions.cs`
- `StorageSignalREventNames.cs`

## Boundaries

- In scope: SignalR connection lifecycle, client events, transfer-progress handling, and public configuration
- Out of scope: server-side hub behavior, HTTP controller flows, and provider implementations
- Protected or high-risk areas: hub event names, payload shapes, and connection disposal behavior shared with the server hub

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.Client.SignalR.csproj`
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

- Keep event names and payload expectations aligned with `StorageHubBase` and `StorageSignalREventNames`.
- Dispose handlers and hub connections cleanly; leaked subscriptions are regressions.
- Do not depend on server internals beyond the published SignalR contract.
