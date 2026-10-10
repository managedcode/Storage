using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.DataLake;

// External Azure protocol simulation: real Azure SDK requests over Kestrel, backed by real files.
// Azurite does not implement the ADLS Gen2 DFS protocol. This is not hosted-Azure qualification.
public sealed class DataLakeProtocolHost : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "adls-contract", Guid.NewGuid().ToString("N"));
    private readonly ConcurrentDictionary<string, bool> _rejected = new();
    private WebApplication _app = null!;
    public string ConnectionString { get; private set; } = string.Empty;
    public void Reject(string filesystem) => _rejected[filesystem] = true;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _app = builder.Build();
        _app.Run(HandleAsync);
        await _app.StartAsync(TestContext.Current.CancellationToken);
        var endpoint = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        ConnectionString = $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey={key};BlobEndpoint={endpoint}/devstoreaccount1;";
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        Directory.Delete(_root, recursive: true);
    }

    private async Task HandleAsync(HttpContext context)
    {
        var segments = context.Request.Path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || segments.Any(s => s is "." or ".."))
        {
            context.Response.StatusCode = 400;
            return;
        }
        var filesystem = segments[1];
        if (_rejected.ContainsKey(filesystem))
        {
            await ErrorAsync(context, 403, "AuthorizationPermissionMismatch");
            return;
        }
        var filesystemPath = Path.Combine(_root, filesystem);
        var path = Path.Combine(_root, Path.Combine(segments.Skip(1).ToArray()));
        context.Response.Headers["x-ms-request-id"] = Guid.NewGuid().ToString("N");
        context.Response.Headers["x-ms-version"] = "2023-11-03";
        switch (context.Request.Method)
        {
            case "PUT": await CreateAsync(context, path, filesystemPath); break;
            case "PATCH": await UpdateAsync(context, path); break;
            case "DELETE":
                if (!File.Exists(path) && !Directory.Exists(path)) { await ErrorAsync(context, 404, "PathNotFound"); break; }
                if (Directory.Exists(path)) Directory.Delete(path, recursive: true); else File.Delete(path);
                context.Response.StatusCode = path == filesystemPath ? 202 : 200;
                break;
            case "HEAD":
            case "GET": await ReadAsync(context, path, filesystemPath); break;
            default: context.Response.StatusCode = 405; break;
        }
    }

    private async Task CreateAsync(HttpContext context, string path, string filesystemPath)
    {
        var rename = context.Request.Headers["x-ms-rename-source"].ToString();
        if (rename.Length > 0)
        {
            var parts = rename.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts[0] == "devstoreaccount1") parts = parts[1..];
            var source = Path.Combine(_root, Path.Combine(parts));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (File.Exists(source)) File.Move(source, path); else Directory.Move(source, path);
        }
        else if (path == filesystemPath || context.Request.Query["resource"] == "directory")
            Directory.CreateDirectory(path);
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var stream = File.Create(path);
        }
        context.Response.StatusCode = 201;
        SetProperties(context, path);
    }

    private static async Task UpdateAsync(HttpContext context, string path)
    {
        if (!File.Exists(path)) { await ErrorAsync(context, 404, "PathNotFound"); return; }
        var position = long.Parse(context.Request.Query["position"].ToString(), CultureInfo.InvariantCulture);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        if (context.Request.Query["action"] == "append")
        {
            stream.Position = position;
            await context.Request.Body.CopyToAsync(stream, context.RequestAborted);
            context.Response.StatusCode = 202;
        }
        else
        {
            stream.SetLength(position);
            context.Response.StatusCode = 200;
        }
        SetProperties(context, path);
    }

    private static async Task ReadAsync(HttpContext context, string path, string filesystemPath)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) { await ErrorAsync(context, 404, "PathNotFound"); return; }
        SetProperties(context, path);
        if (context.Request.Method == "HEAD") return;
        if (context.Request.Query["resource"] == "filesystem")
        {
            var directory = context.Request.Query["directory"].ToString();
            var scan = Path.Combine(filesystemPath, directory);
            var paths = Directory.Exists(scan) ? Directory.GetFileSystemEntries(scan, "*", SearchOption.AllDirectories) : Array.Empty<string>();
            await context.Response.WriteAsJsonAsync(new
            {
                paths = paths.Select(p => new
                {
                    name = Path.GetRelativePath(filesystemPath, p).Replace('\\', '/'),
                    isDirectory = Directory.Exists(p) ? "true" : "false",
                    contentLength = File.Exists(p) ? new FileInfo(p).Length.ToString(CultureInfo.InvariantCulture) : "0",
                    lastModified = File.GetLastWriteTimeUtc(p).ToString("R", CultureInfo.InvariantCulture),
                    etag = "\"test\""
                })
            }, context.RequestAborted);
            return;
        }
        var bytes = await File.ReadAllBytesAsync(path, context.RequestAborted);
        var range = context.Request.Headers["x-ms-range"].ToString();
        if (range.Length == 0) range = context.Request.Headers.Range.ToString();
        if (range.StartsWith("bytes=", StringComparison.Ordinal))
        {
            var limits = range[6..].Split('-');
            var start = int.Parse(limits[0], CultureInfo.InvariantCulture);
            var end = limits[1].Length > 0 ? Math.Min(int.Parse(limits[1], CultureInfo.InvariantCulture), bytes.Length - 1) : bytes.Length - 1;
            context.Response.StatusCode = 206;
            context.Response.Headers.ContentRange = $"bytes {start}-{end}/{bytes.Length}";
            bytes = bytes[start..(end + 1)];
        }
        context.Response.ContentLength = bytes.Length;
        await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
    }

    private static void SetProperties(HttpContext context, string path)
    {
        var modified = File.GetLastWriteTimeUtc(path).ToString("R", CultureInfo.InvariantCulture);
        context.Response.Headers.ETag = "\"test\"";
        context.Response.Headers.LastModified = modified;
        context.Response.Headers["x-ms-creation-time"] = modified;
        context.Response.Headers["x-ms-resource-type"] = Directory.Exists(path) ? "directory" : "file";
        context.Response.Headers["x-ms-blob-type"] = "BlockBlob";
        context.Response.Headers["x-ms-lease-status"] = "unlocked";
        context.Response.Headers["x-ms-lease-state"] = "available";
        context.Response.ContentType = "application/octet-stream";
        if (context.Request.Method == "HEAD") context.Response.ContentLength = File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    private static async Task ErrorAsync(HttpContext context, int status, string code)
    {
        context.Response.StatusCode = status;
        context.Response.Headers["x-ms-error-code"] = code;
        if (context.Request.Method != "HEAD")
            await context.Response.WriteAsJsonAsync(new { error = new { code, message = code } }, context.RequestAborted);
    }
}
