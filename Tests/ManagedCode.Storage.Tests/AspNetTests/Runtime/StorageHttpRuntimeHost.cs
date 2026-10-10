using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Server.Controllers;
using ManagedCode.Storage.Server.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.AspNetTests.Runtime;

public sealed class StorageHttpRuntimeHost : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "storage-http", Guid.NewGuid().ToString("N"));
    private WebApplication _app = null!;
    public HttpClient Client { get; private set; } = null!;
    public IStorage Storage => _app.Services.GetRequiredService<IStorage>();

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(RuntimeStorageController).Assembly);
        builder.Services.AddFileSystemStorageAsDefault(new FileSystemStorageOptions { BaseFolder = _root });
        builder.Services.AddStorageServer();
        builder.Services.AddSingleton(new StorageServerOptions
        {
            MaxFileSize = 16 * 1024,
            MaxChunkSize = 8 * 1024,
            InMemoryUploadThresholdBytes = 4
        });
        _app = builder.Build();
        _app.MapControllers();
        await _app.StartAsync(TestContext.Current.CancellationToken);
        (await Storage.CreateContainerAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var endpoint = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        Client = new HttpClient { BaseAddress = new Uri(endpoint) };
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
