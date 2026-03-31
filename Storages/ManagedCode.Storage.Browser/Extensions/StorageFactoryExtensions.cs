using System;
using ManagedCode.Storage.Browser.Options;
using ManagedCode.Storage.Core.Providers;
using Microsoft.JSInterop;

namespace ManagedCode.Storage.Browser.Extensions;

public static class StorageFactoryExtensions
{
    public static IBrowserStorage CreateBrowserStorage(this IStorageFactory factory, IJSRuntime jsRuntime, BrowserStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(options);

        options.JsRuntime = jsRuntime;
        return factory.CreateStorage<IBrowserStorage, BrowserStorageOptions>(options);
    }

    public static IBrowserStorage CreateBrowserStorage(this IStorageFactory factory, IJSRuntime jsRuntime, Action<BrowserStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(configure);

        return factory.CreateStorage<IBrowserStorage, BrowserStorageOptions>(options =>
        {
            options.JsRuntime = jsRuntime;
            configure(options);
        });
    }
}
