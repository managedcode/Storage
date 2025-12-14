using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Drive.v3.Data;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Dropbox;
using ManagedCode.Storage.Dropbox.Clients;
using ManagedCode.Storage.Dropbox.Options;
using ManagedCode.Storage.GoogleDrive;
using ManagedCode.Storage.GoogleDrive.Clients;
using ManagedCode.Storage.GoogleDrive.Options;
using ManagedCode.Storage.OneDrive;
using ManagedCode.Storage.OneDrive.Clients;
using ManagedCode.Storage.OneDrive.Options;
using Microsoft.Graph.Models;
using Shouldly;
using Xunit;
using File = Google.Apis.Drive.v3.Data.File;

namespace ManagedCode.Storage.Tests.Storages.CloudDrive;

public class CloudDriveStorageTests
{
    [Fact]
    public async Task OneDrive_FakeClient_RoundTrip()
    {
        var fakeClient = new FakeOneDriveClient();
        var storage = new OneDriveStorage(new OneDriveStorageOptions
        {
            Client = fakeClient,
            DriveId = "drive",
            RootPath = "root"
        });

        var uploadResult = await storage.UploadAsync("hello world", options => options.FileName = "text.txt");
        uploadResult.IsSuccess.ShouldBeTrue();

        var exists = await storage.ExistsAsync("text.txt");
        exists.Value.ShouldBeTrue();

        var metadata = await storage.GetBlobMetadataAsync("text.txt");
        metadata.Value.Name.ShouldBe("text.txt");

        var download = await storage.DownloadAsync("text.txt");
        using var reader = new StreamReader(download.Value.FileStream);
        (await reader.ReadToEndAsync()).ShouldBe("hello world");

        var listed = new List<BlobMetadata>();
        await foreach (var item in storage.GetBlobMetadataListAsync())
        {
            listed.Add(item);
        }

        listed.ShouldContain(m => m.FullName.EndsWith("text.txt"));
    }

    [Fact]
    public async Task GoogleDrive_FakeClient_RoundTrip()
    {
        var fakeClient = new FakeGoogleDriveClient();
        var storage = new GoogleDriveStorage(new GoogleDriveStorageOptions
        {
            Client = fakeClient,
            RootFolderId = "root"
        });

        var uploadResult = await storage.UploadAsync("drive content", options => options.FileName = "data.bin");
        uploadResult.IsSuccess.ShouldBeTrue();

        var exists = await storage.ExistsAsync("data.bin");
        exists.Value.ShouldBeTrue();

        var metadata = await storage.GetBlobMetadataAsync("data.bin");
        metadata.Value.FullName.ShouldBe("data.bin");

        var download = await storage.DownloadAsync("data.bin");
        using var reader = new StreamReader(download.Value.FileStream);
        (await reader.ReadToEndAsync()).ShouldBe("drive content");

        var listed = new List<BlobMetadata>();
        await foreach (var item in storage.GetBlobMetadataListAsync())
        {
            listed.Add(item);
        }

        listed.ShouldContain(m => m.FullName.Contains("data.bin"));
    }

    [Fact]
    public async Task Dropbox_FakeClient_RoundTrip()
    {
        var fakeClient = new FakeDropboxClient();
        var storage = new DropboxStorage(new DropboxStorageOptions
        {
            Client = fakeClient,
            RootPath = "/apps/demo"
        });

        var uploadResult = await storage.UploadAsync("dropbox payload", options => options.FileName = "file.json");
        uploadResult.IsSuccess.ShouldBeTrue();

        var exists = await storage.ExistsAsync("file.json");
        exists.Value.ShouldBeTrue();

        var metadata = await storage.GetBlobMetadataAsync("file.json");
        metadata.Value.Name.ShouldBe("file.json");

        var download = await storage.DownloadAsync("file.json");
        using var reader = new StreamReader(download.Value.FileStream);
        (await reader.ReadToEndAsync()).ShouldBe("dropbox payload");

        var listed = new List<BlobMetadata>();
        await foreach (var item in storage.GetBlobMetadataListAsync())
        {
            listed.Add(item);
        }

        listed.ShouldContain(m => m.FullName.Contains("file.json"));
    }

    private class FakeOneDriveClient : IOneDriveClient
    {
        private readonly InMemoryDrive _drive = new();

        public Task EnsureRootAsync(string driveId, string rootPath, bool createIfNotExists, CancellationToken cancellationToken)
        {
            _drive.Root = rootPath;
            return Task.CompletedTask;
        }

        public Task<DriveItem> UploadAsync(string driveId, string path, Stream content, string? contentType, CancellationToken cancellationToken)
        {
            var entry = _drive.Save(path, content, contentType);
            return Task.FromResult(entry.ToDriveItem(path));
        }

        public Task<Stream> DownloadAsync(string driveId, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Download(path));
        }

        public Task<bool> DeleteAsync(string driveId, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Delete(path));
        }

        public Task<bool> ExistsAsync(string driveId, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Exists(path));
        }

        public Task<DriveItem?> GetMetadataAsync(string driveId, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Get(path)?.ToDriveItem(path));
        }

        public async IAsyncEnumerable<DriveItem> ListAsync(string driveId, string? directory, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var entry in _drive.List(directory, cancellationToken))
            {
                yield return entry.ToDriveItem(entry.Path);
            }
        }
    }

    private class FakeGoogleDriveClient : IGoogleDriveClient
    {
        private readonly InMemoryDrive _drive = new();

        public Task EnsureRootAsync(string rootFolderId, bool createIfNotExists, CancellationToken cancellationToken)
        {
            _drive.Root = rootFolderId;
            return Task.CompletedTask;
        }

        public Task<File> UploadAsync(string rootFolderId, string path, Stream content, string? contentType, CancellationToken cancellationToken)
        {
            var entry = _drive.Save(path, content, contentType);
            return Task.FromResult(entry.ToGoogleFile(path));
        }

        public Task<Stream> DownloadAsync(string rootFolderId, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Download(path));
        }

        public Task<bool> DeleteAsync(string rootFolderId, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Delete(path));
        }

        public Task<bool> ExistsAsync(string rootFolderId, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Exists(path));
        }

        public Task<File?> GetMetadataAsync(string rootFolderId, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Get(path)?.ToGoogleFile(path));
        }

        public async IAsyncEnumerable<File> ListAsync(string rootFolderId, string? directory, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var entry in _drive.List(directory, cancellationToken))
            {
                yield return entry.ToGoogleFile(entry.Path);
            }
        }
    }

    private class FakeDropboxClient : IDropboxClientWrapper
    {
        private readonly InMemoryDrive _drive = new();

        public Task EnsureRootAsync(string rootPath, bool createIfNotExists, CancellationToken cancellationToken)
        {
            _drive.Root = rootPath;
            return Task.CompletedTask;
        }

        public Task<DropboxItemMetadata> UploadAsync(string rootPath, string path, Stream content, string? contentType, CancellationToken cancellationToken)
        {
            var entry = _drive.Save(path, content, contentType);
            return Task.FromResult(entry.ToDropboxFile(path));
        }

        public Task<Stream> DownloadAsync(string rootPath, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Download(path));
        }

        public Task<bool> DeleteAsync(string rootPath, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Delete(path));
        }

        public Task<bool> ExistsAsync(string rootPath, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Exists(path));
        }

        public Task<DropboxItemMetadata?> GetMetadataAsync(string rootPath, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult<DropboxItemMetadata?>(_drive.Get(path)?.ToDropboxFile(path));
        }

        public async IAsyncEnumerable<DropboxItemMetadata> ListAsync(string rootPath, string? directory, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var entry in _drive.List(directory, cancellationToken))
            {
                yield return entry.ToDropboxFile(entry.Path);
            }
        }
    }

    private class InMemoryDrive
    {
        private readonly Dictionary<string, DriveEntry> _entries = new();

        public string Root { get; set; } = string.Empty;

        public DriveEntry Save(string path, Stream content, string? contentType)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            var data = ms.ToArray();
            var entry = new DriveEntry
            {
                Content = data,
                ContentType = contentType ?? "application/octet-stream",
                Created = System.DateTimeOffset.UtcNow,
                Updated = System.DateTimeOffset.UtcNow,
                Path = Normalize(path)
            };

            _entries[entry.Path] = entry;
            return entry;
        }

        public bool Delete(string path)
        {
            return _entries.Remove(Normalize(path));
        }

        public bool Exists(string path)
        {
            return _entries.ContainsKey(Normalize(path));
        }

        public DriveEntry? Get(string path)
        {
            return _entries.TryGetValue(Normalize(path), out var entry) ? entry : null;
        }

        public Stream Download(string path)
        {
            var normalized = Normalize(path);
            if (!_entries.TryGetValue(normalized, out var entry))
            {
                throw new FileNotFoundException(path);
            }

            return new MemoryStream(entry.Content, writable: false);
        }

        public async IAsyncEnumerable<DriveEntry> List(string? directory, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var normalized = string.IsNullOrWhiteSpace(directory) ? null : Normalize(directory!);
            foreach (var entry in _entries.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (normalized == null || entry.Path.StartsWith(normalized))
                {
                    yield return entry;
                }
            }

            await Task.CompletedTask;
        }

        private string Normalize(string path)
        {
            return path.Replace("\\", "/").Trim('/');
        }
    }

    internal class DriveEntry
    {
        public required string Path { get; set; }
        public required byte[] Content { get; set; }
        public required string ContentType { get; set; }
        public required System.DateTimeOffset Created { get; set; }
        public required System.DateTimeOffset Updated { get; set; }
    }
}

internal static class DriveEntryExtensions
{
    public static DriveItem ToDriveItem(this CloudDriveStorageTests.DriveEntry entry, string fullPath)
    {
        return new DriveItem
        {
            Name = System.IO.Path.GetFileName(fullPath),
            Size = entry.Content.LongLength,
            CreatedDateTime = entry.Created,
            LastModifiedDateTime = entry.Updated
        };
    }

    public static File ToGoogleFile(this CloudDriveStorageTests.DriveEntry entry, string fullPath)
    {
        return new File
        {
            Name = System.IO.Path.GetFileName(fullPath),
            Size = entry.Content.LongLength,
            CreatedTimeDateTimeOffset = entry.Created,
            ModifiedTimeDateTimeOffset = entry.Updated,
            MimeType = entry.ContentType
        };
    }

    public static DropboxItemMetadata ToDropboxFile(this CloudDriveStorageTests.DriveEntry entry, string fullPath)
    {
        return new DropboxItemMetadata
        {
            Name = System.IO.Path.GetFileName(fullPath),
            Path = entry.Path,
            Size = (ulong)entry.Content.LongLength,
            ClientModified = entry.Created.UtcDateTime,
            ServerModified = entry.Updated.UtcDateTime
        };
    }
}
