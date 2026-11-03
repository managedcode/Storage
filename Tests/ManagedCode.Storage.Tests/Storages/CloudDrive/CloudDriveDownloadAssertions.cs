using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using Shouldly;

namespace ManagedCode.Storage.Tests.Storages.CloudDrive;

internal static class CloudDriveDownloadAssertions
{
    internal static async Task ReplacesPreviousContentAsync(IStorage storage, string key, string expected, CancellationToken token)
    {
        var path = Path.Combine(Environment.CurrentDirectory, $"cloud-drive-{Guid.NewGuid():N}");
        try
        {
            await File.WriteAllTextAsync(path, new string('x', expected.Length * 2), token);
            var download = await storage.DownloadAsync(new DownloadOptions { FileName = key, LocalPath = path }, token);
            download.IsSuccess.ShouldBeTrue(download.Problem?.ToString());
            await using var file = download.Value;
            (await file.ReadAllTextAsync(token)).ShouldBe(expected);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
