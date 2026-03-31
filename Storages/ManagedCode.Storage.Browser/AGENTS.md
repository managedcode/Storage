Project: ManagedCode.Storage.Browser
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Provides an `IStorage` implementation with IndexedDB metadata and OPFS-backed payload storage.
- Exists so browser-facing .NET applications can reuse the repository storage abstraction for client-local streamed payloads, with Blazor DI helpers and MVC asset helpers in one package.

## Entry Points

- `IBrowserStorage.cs`
- `BrowserStorage.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Extensions/StorageFactoryExtensions.cs`
- `Options/BrowserStorageOptions.cs`
- `Mvc/BrowserStorageStaticAssetPaths.cs`
- `wwwroot/browserStorage.js`

## Boundaries

- In scope: browser-local persistence over `IJSRuntime`, IndexedDB metadata, OPFS payload streaming, Blazor DI registration, and MVC static-asset helper classes
- Out of scope: protected/encrypted browser storage, background sync, and server-only `ProtectedBrowserStorage`
- Protected or high-risk areas: `IJSRuntime` lifecycle, prerendering boundaries, chunked stream correctness, and JS module contract stability

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.Browser.csproj`
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
- Required local docs: `docs/Architecture.md`, `README.md`, and the nearest feature docs when public behavior changes.
- Local exception policy: browser storage tests must run through real browser hosts; do not add fake browser runtimes or in-process JS stubs.

## Local Rules

- Register `IBrowserStorage` and default `IStorage` as `Scoped` because `IJSRuntime` is scoped in Blazor.
- Do not claim support during prerendering; browser storage is only available after the app becomes interactive.
- Keep one shared browser script contract so Blazor interop and MVC asset usage stay aligned.
