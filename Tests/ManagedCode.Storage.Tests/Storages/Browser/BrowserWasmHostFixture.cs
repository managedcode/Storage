using System.IO;

namespace ManagedCode.Storage.Tests.Storages.Browser;

public sealed class BrowserWasmHostFixture() : BrowserPlaywrightHostFixtureBase(
    Path.Combine("Tests", "ManagedCode.Storage.BrowserWasmHost", "ManagedCode.Storage.BrowserWasmHost.csproj"),
    "playwright-browser-storage-wasm",
    "playwright-browser-storage-wasm");
