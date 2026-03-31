---
keywords: "browser storage, opfs, ManagedCode.Storage.Browser, IStorage, IJSRuntime, IndexedDB metadata, Blazor, MVC, .NET"
---

# Feature: Browser Storage Provider (`ManagedCode.Storage.Browser`)

## Purpose

Implement `IStorage` on top of browser storage primitives so browser-facing .NET applications can persist client-local payloads behind the same storage abstraction used by the rest of the repository:

- per-browser persistence across reloads and browser restarts
- `IndexedDB` metadata plus OPFS-backed payloads
- DI-friendly storage access inside Blazor components and scoped services
- one shared browser script contract that can also be referenced from ASP.NET MVC or Razor Pages

> This provider targets browser storage, not protected storage. Data remains user-visible and user-modifiable, and browser APIs are unavailable during prerendering.

## Main Flows

```mermaid
flowchart LR
  App --> Storage["BrowserStorage : IBrowserStorage"]
  Storage --> JS["IJSRuntime + JS module"]
  JS --> Browser["IndexedDB metadata + OPFS payload files"]
```

## Components

- `Storages/ManagedCode.Storage.Browser/BrowserStorage.cs`
- `Storages/ManagedCode.Storage.Browser/BrowserStorageProvider.cs`
- DI:
  - `Storages/ManagedCode.Storage.Browser/Extensions/ServiceCollectionExtensions.cs`
  - `Storages/ManagedCode.Storage.Browser/Extensions/StorageFactoryExtensions.cs`
- Options:
  - `Storages/ManagedCode.Storage.Browser/Options/BrowserStorageOptions.cs`
- JS module:
  - `Storages/ManagedCode.Storage.Browser/wwwroot/browserStorage.js`
- MVC or Razor asset helper:
  - `Storages/ManagedCode.Storage.Browser/Mvc/BrowserStorageStaticAssetPaths.cs`

## DI Wiring

```bash
dotnet add package ManagedCode.Storage.Browser
```

```csharp
using ManagedCode.Storage.Browser.Extensions;

builder.Services.AddBrowserStorageAsDefault(options =>
{
    options.ContainerName = "drafts";
    options.DatabaseName = "managedcode-storage";
    options.ChunkSizeBytes = 4 * 1024 * 1024;
    options.ChunkBatchSize = 4;
});
```

Inject the typed provider or default `IStorage` in a Blazor-scoped service or component after the app becomes interactive:

```csharp
public sealed class DraftService(IBrowserStorage storage)
{
    public Task<Result<BlobMetadata>> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        return storage.UploadAsync(content, new UploadOptions
        {
            FileName = "draft.json",
            MimeType = "application/json"
        }, cancellationToken);
    }
}
```

Recommended tuning for larger browser-local payloads:

- `ChunkSizeBytes = 4 * 1024 * 1024`
- `ChunkBatchSize = 4`
- Blazor Server or Interactive Server: `HubOptions.MaximumReceiveMessageSize >= 32L * 1024 * 1024`

If the same application also uses Interactive Server rendering, keep the SignalR receive limit above the full browser read window because browser-to-server JS interop responses are capped by `HubOptions.MaximumReceiveMessageSize`:

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 32L * 1024 * 1024;
    });
```

## Current Behavior

- Uses a package-owned JS module and `IJSRuntime`; consumers don't need to author a helper script.
- Registers `IBrowserStorage` and default `IStorage` as `Scoped` because browser storage depends on scoped `IJSRuntime`.
- Maps directories and file names into a namespaced key space inside browser `IndexedDB`.
- Stores blob metadata separately from payload bytes so list and existence operations don't have to materialize file contents.
- Stores payload bytes in OPFS and keeps `IndexedDB` focused on metadata only.
- Requires OPFS for payload writes and fails fast when the browser does not expose the required OPFS APIs.
- Uploads streams chunk-by-chunk, can batch multiple contiguous chunks into one JS interop window, and exposes a lazy OPFS-backed `Stream`.
- Exposes a stable static asset path so MVC or Razor Pages apps can reference the same browser script contract.
- Supports `ManagedCode.Storage.VirtualFileSystem` on top of the same provider without a browser-specific VFS fork; the real browser hosts exercise small-file save and overwrite flows, read, list, move, delete, large-payload roundtrips, and multi-tab flows end to end.
- The browser host playgrounds emit progress logs every `100 MiB` for the `1 GiB` save and load verification flows.

## Caveats

- Browser storage isn't available during prerendering. In Blazor Server, wait until `OnAfterRenderAsync(firstRender: true)` or disable prerendering for the affected subtree.
- `IndexedDB` is shared across tabs for the same origin.
- OPFS is origin-scoped and is cleared with site data; this provider now depends on OPFS for payload storage.
- Data is readable and mutable by the user; don't store secrets or sensitive data there.
- Blazor WebAssembly is the primary runtime for heavier browser-local media flows because it avoids the server-side SignalR hop.
- In Interactive Server mode, browser-to-server JS interop responses are limited by `HubOptions.MaximumReceiveMessageSize`; keep it aligned with `ChunkSizeBytes * ChunkBatchSize` plus headroom for interop framing.
- Browser quotas still apply. Practical limits depend on the origin quota the browser grants to IndexedDB metadata and OPFS files.

## Tests

- `Tests/ManagedCode.Storage.Tests/Storages/Browser/BrowserServerStorageIntegrationTests.cs`
- `Tests/ManagedCode.Storage.Tests/Storages/Browser/BrowserWasmStorageIntegrationTests.cs`
- `Tests/ManagedCode.Storage.Tests/Storages/Browser/BrowserServerVfsIntegrationTests.cs`
- `Tests/ManagedCode.Storage.Tests/Storages/Browser/BrowserWasmVfsIntegrationTests.cs`
- `Tests/ManagedCode.Storage.Tests/Storages/Browser/BrowserStoragePage.cs`
- `Tests/ManagedCode.Storage.Tests/Storages/Browser/BrowserServerHostFixture.cs`
- `Tests/ManagedCode.Storage.Tests/Storages/Browser/BrowserWasmHostFixture.cs`
- `Tests/ManagedCode.Storage.BrowserServerHost/`
- `Tests/ManagedCode.Storage.BrowserWasmHost/`
