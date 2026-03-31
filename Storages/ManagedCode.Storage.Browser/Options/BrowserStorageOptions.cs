using ManagedCode.Storage.Core;
using Microsoft.JSInterop;

namespace ManagedCode.Storage.Browser.Options;

public sealed class BrowserStorageOptions : IStorageOptions
{
    public string ContainerName { get; set; } = "managedcode-browser-storage";

    public string DatabaseName { get; set; } = "managedcode-browser-storage";

    public int ChunkSizeBytes { get; set; } = 1024 * 1024;

    public int ChunkBatchSize { get; set; } = 1;

    public bool CreateContainerIfNotExists { get; set; } = true;

    public IJSRuntime? JsRuntime { get; set; }
}
