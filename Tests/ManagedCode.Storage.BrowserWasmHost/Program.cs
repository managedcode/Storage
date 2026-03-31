using ManagedCode.Storage.Browser.Extensions;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using ManagedCode.Storage.BrowserWasmHost;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

const int BrowserChunkSizeBytes = 4 * 1024 * 1024;
const int BrowserChunkBatchSize = 4;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBrowserStorageAsDefault(options =>
{
    options.ContainerName = "playwright-browser-storage-wasm";
    options.DatabaseName = "playwright-browser-storage-wasm";
    options.ChunkSizeBytes = BrowserChunkSizeBytes;
    options.ChunkBatchSize = BrowserChunkBatchSize;
});
builder.Services.AddVirtualFileSystem(options =>
{
    options.DefaultContainer = "playwright-browser-storage-wasm";
});

await builder.Build().RunAsync();
