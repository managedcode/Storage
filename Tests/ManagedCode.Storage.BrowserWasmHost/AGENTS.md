Project: ManagedCode.Storage.BrowserWasmHost
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Provides a minimal standalone Blazor WebAssembly host used to verify `ManagedCode.Storage.Browser` in a real browser.
- Exists for Playwright-driven end-to-end checks against browser storage with IndexedDB metadata and OPFS payloads in the pure WASM execution model.

## Entry Points

- `Program.cs`
- `App.razor`
- `Pages/Home.razor`
- `Pages/StoragePlayground.razor`
- `wwwroot/index.html`

## Boundaries

- In scope: minimal WASM host wiring, storage playground UI, and browser-verifiable flows
- Out of scope: production UX, authentication, service workers, or generic scaffolding beyond the provider test surface
- Protected or high-risk areas: deterministic element ids used by Playwright and the static boot page required for standalone WASM startup

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.BrowserWasmHost.csproj`
- `run`: `dotnet run --project ManagedCode.Storage.BrowserWasmHost.csproj`
- `format`: `dotnet format ../../ManagedCode.Storage.slnx`
- Analyzer severity lives in the repo-root `.editorconfig`.

## Applicable Skills

- `mcaf-dotnet`
- `mcaf-testing`
- `playwright`

## Local Rules

- Keep the host hand-written and minimal; do not regenerate template scaffolding.
- Preserve stable element ids and text hooks because Playwright automation depends on them.
- Keep the host focused on real browser verification, not feature demonstrations.
