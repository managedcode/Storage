Project: ManagedCode.Storage.Server
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Provides ASP.NET controllers, hubs, helpers, and DI extensions for exposing storage operations over HTTP and SignalR.
- Exists so consumers can inherit minimal base endpoints and host upload, download, and streaming flows without duplicating transport plumbing.

## Entry Points

- `Controllers/StorageControllerBase.cs`
- `Controllers/StorageController.cs`
- `Hubs/StorageHubBase.cs`
- `Hubs/StorageHub.cs`
- `Extensions/DependencyInjection/StorageServerBuilderExtensions.cs`

## Boundaries

- In scope: controller and hub contracts, upload or download helpers, chunked transfer wiring, and server-side DI setup
- Out of scope: provider SDK logic, HTTP client behavior, and Orleans grain persistence
- Protected or high-risk areas: base controller or hub extensibility, route surface, streaming behavior, and chunk-upload semantics

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.Server.csproj`
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

- Base controllers and hubs must stay minimal and customizable; do not hardcode routes, auth policy, or provider-specific behavior.
- Preserve streaming and chunked-upload paths as incremental flows; do not add whole-file buffering.
- Keep server contracts aligned with both HTTP and SignalR client packages.
