Project: ManagedCode.Storage.BrowserServerHost
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Provides a minimal interactive Blazor Server host used to verify `ManagedCode.Storage.Browser` in a real browser.
- Exists for Playwright-driven end-to-end checks against browser storage with IndexedDB metadata, OPFS payloads, server-side interactivity, and SignalR-backed JS interop.

## Entry Points

- `Program.cs`
- `Components/App.razor`
- `Components/Routes.razor`
- `Components/Pages/StoragePlayground.razor`

## Boundaries

- In scope: minimal host wiring, storage playground UI, and browser-verifiable flows
- Out of scope: production UX, authentication, or generic application scaffolding beyond the provider test surface
- Protected or high-risk areas: interactive server render mode, prerender settings, SignalR receive-message limits for browser-to-server chunks, and deterministic element ids used by Playwright

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.BrowserServerHost.csproj`
- `run`: `dotnet run --project ManagedCode.Storage.BrowserServerHost.csproj`
- `format`: `dotnet format ../../ManagedCode.Storage.slnx`
- Analyzer severity lives in the repo-root `.editorconfig`.

## Applicable Skills

- `mcaf-dotnet`
- `mcaf-testing`
- `playwright`

## Local Rules

- Keep prerendering disabled for the interactive test surface so browser storage is available immediately.
- Keep `HubOptions.MaximumReceiveMessageSize` aligned with the browser chunk size so browser-to-server reads don't stall on default 32 KB SignalR limits.
- Preserve stable element ids and text hooks because Playwright automation depends on them.
- Keep the host minimal; its job is verification, not product feature development.
