using ManagedCode.Storage.Browser.Extensions;
using ManagedCode.Storage.BrowserServerHost.Components;
using ManagedCode.Storage.VirtualFileSystem.Extensions;

const int BrowserChunkSizeBytes = 4 * 1024 * 1024;
const int BrowserChunkBatchSize = 4;
const long BrowserSignalRWindowBytes = 32L * 1024 * 1024;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.EnableDetailedErrors = true;
        options.MaximumReceiveMessageSize = BrowserSignalRWindowBytes;
    });

builder.Services.AddBrowserStorageAsDefault(options =>
{
    options.ContainerName = "playwright-browser-storage-server";
    options.DatabaseName = "playwright-browser-storage-server";
    options.ChunkSizeBytes = BrowserChunkSizeBytes;
    options.ChunkBatchSize = BrowserChunkBatchSize;
});
builder.Services.AddVirtualFileSystem(options =>
{
    options.DefaultContainer = "playwright-browser-storage-server";
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
