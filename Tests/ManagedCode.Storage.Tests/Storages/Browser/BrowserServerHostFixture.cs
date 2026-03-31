using System.IO;

namespace ManagedCode.Storage.Tests.Storages.Browser;

public sealed class BrowserServerHostFixture() : BrowserPlaywrightHostFixtureBase(
    Path.Combine("Tests", "ManagedCode.Storage.BrowserServerHost", "ManagedCode.Storage.BrowserServerHost.csproj"),
    "playwright-browser-storage-server",
    "playwright-browser-storage-server");
