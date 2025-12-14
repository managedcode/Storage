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
        uploadResult.Value.FullName.ShouldBe("text.txt");
        uploadResult.Value.Container.ShouldBe("drive");

        var exists = await storage.ExistsAsync("text.txt");
        exists.IsSuccess.ShouldBeTrue();
        exists.Value.ShouldBeTrue();

        var metadata = await storage.GetBlobMetadataAsync("text.txt");
        metadata.IsSuccess.ShouldBeTrue();
        metadata.Value.Name.ShouldBe("text.txt");
        metadata.Value.FullName.ShouldBe("text.txt");
        metadata.Value.Container.ShouldBe("drive");

        var download = await storage.DownloadAsync("text.txt");
        download.IsSuccess.ShouldBeTrue();
        using var reader = new StreamReader(download.Value.FileStream);
        (await reader.ReadToEndAsync()).ShouldBe("hello world");

        var listed = new List<BlobMetadata>();
        await foreach (var item in storage.GetBlobMetadataListAsync())
        {
            listed.Add(item);
        }

        listed.ShouldContain(m => m.FullName == "text.txt");
    }

    [Fact]
    public async Task OneDrive_RemoveContainer_NotSupported()
    {
        var fakeClient = new FakeOneDriveClient();
        var storage = new OneDriveStorage(new OneDriveStorageOptions
        {
            Client = fakeClient,
            DriveId = "drive",
            RootPath = "root"
        });

        var result = await storage.RemoveContainerAsync();
        result.IsSuccess.ShouldBeFalse();
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
        uploadResult.Value.FullName.ShouldBe("data.bin");
        uploadResult.Value.Container.ShouldBe("root");

        var exists = await storage.ExistsAsync("data.bin");
        exists.IsSuccess.ShouldBeTrue();
        exists.Value.ShouldBeTrue();

        var metadata = await storage.GetBlobMetadataAsync("data.bin");
        metadata.IsSuccess.ShouldBeTrue();
        metadata.Value.FullName.ShouldBe("data.bin");
        metadata.Value.Container.ShouldBe("root");

        var download = await storage.DownloadAsync("data.bin");
        download.IsSuccess.ShouldBeTrue();
        using var reader = new StreamReader(download.Value.FileStream);
        (await reader.ReadToEndAsync()).ShouldBe("drive content");

        var listed = new List<BlobMetadata>();
        await foreach (var item in storage.GetBlobMetadataListAsync())
        {
            listed.Add(item);
        }

        listed.ShouldContain(m => m.FullName == "data.bin");
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
        uploadResult.Value.FullName.ShouldBe("file.json");
        uploadResult.Value.Container.ShouldBe("/apps/demo");
        uploadResult.Value.MimeType.ShouldBe("application/json");

        var exists = await storage.ExistsAsync("file.json");
        exists.IsSuccess.ShouldBeTrue();
        exists.Value.ShouldBeTrue();

        var metadata = await storage.GetBlobMetadataAsync("file.json");
        metadata.IsSuccess.ShouldBeTrue();
        metadata.Value.Name.ShouldBe("file.json");
        metadata.Value.FullName.ShouldBe("file.json");
        metadata.Value.Container.ShouldBe("/apps/demo");
        metadata.Value.MimeType.ShouldBe("application/json");

        var download = await storage.DownloadAsync("file.json");
        download.IsSuccess.ShouldBeTrue();
        using var reader = new StreamReader(download.Value.FileStream);
        (await reader.ReadToEndAsync()).ShouldBe("dropbox payload");

        var listed = new List<BlobMetadata>();
        await foreach (var item in storage.GetBlobMetadataListAsync())
        {
            listed.Add(item);
        }

        listed.ShouldContain(m => m.FullName == "file.json");
    }

    [Fact]
    public async Task OneDrive_DeleteDirectory_ShouldDeleteOnlyDirectoryContent()
    {
        var fakeClient = new FakeOneDriveClient();
        var storage = new OneDriveStorage(new OneDriveStorageOptions
        {
            Client = fakeClient,
            DriveId = "drive",
            RootPath = "root"
        });

        (await storage.UploadAsync("dir-1", options =>
        {
            options.Directory = "dir";
            options.FileName = "a.txt";
        })).IsSuccess.ShouldBeTrue();

        (await storage.UploadAsync("dir-2", options =>
        {
            options.Directory = "dir";
            options.FileName = "b.txt";
        })).IsSuccess.ShouldBeTrue();

        (await storage.UploadAsync("keep", options => options.FileName = "keep.txt")).IsSuccess.ShouldBeTrue();

        var deleteResult = await storage.DeleteDirectoryAsync("dir");
        deleteResult.IsSuccess.ShouldBeTrue();

        var dirAExists = await storage.ExistsAsync("dir/a.txt");
        dirAExists.IsSuccess.ShouldBeTrue();
        dirAExists.Value.ShouldBeFalse();

        var keepExists = await storage.ExistsAsync("keep.txt");
        keepExists.IsSuccess.ShouldBeTrue();
        keepExists.Value.ShouldBeTrue();

        var listed = new List<BlobMetadata>();
        await foreach (var item in storage.GetBlobMetadataListAsync("dir"))
        {
            listed.Add(item);
        }

        listed.ShouldBeEmpty();
    }

    [Fact]
    public async Task GoogleDrive_DeleteDirectory_ShouldDeleteOnlyDirectoryContent()
    {
        var fakeClient = new FakeGoogleDriveClient();
        var storage = new GoogleDriveStorage(new GoogleDriveStorageOptions
        {
            Client = fakeClient,
            RootFolderId = "root"
        });

        (await storage.UploadAsync("dir-1", options =>
        {
            options.Directory = "dir";
            options.FileName = "a.txt";
        })).IsSuccess.ShouldBeTrue();

        (await storage.UploadAsync("dir-2", options =>
        {
            options.Directory = "dir";
            options.FileName = "b.txt";
        })).IsSuccess.ShouldBeTrue();

        (await storage.UploadAsync("keep", options => options.FileName = "keep.txt")).IsSuccess.ShouldBeTrue();

        var deleteResult = await storage.DeleteDirectoryAsync("dir");
        deleteResult.IsSuccess.ShouldBeTrue();

        var dirAExists = await storage.ExistsAsync("dir/a.txt");
        dirAExists.IsSuccess.ShouldBeTrue();
        dirAExists.Value.ShouldBeFalse();

        var keepExists = await storage.ExistsAsync("keep.txt");
        keepExists.IsSuccess.ShouldBeTrue();
        keepExists.Value.ShouldBeTrue();

        var listed = new List<BlobMetadata>();
        await foreach (var item in storage.GetBlobMetadataListAsync("dir"))
        {
            listed.Add(item);
        }

        listed.ShouldBeEmpty();
    }

    [Fact]
    public async Task Dropbox_DeleteDirectory_ShouldDeleteOnlyDirectoryContent()
    {
        var fakeClient = new FakeDropboxClient();
        var storage = new DropboxStorage(new DropboxStorageOptions
        {
            Client = fakeClient,
            RootPath = "/apps/demo"
        });

        (await storage.UploadAsync("dir-1", options =>
        {
            options.Directory = "dir";
            options.FileName = "a.txt";
        })).IsSuccess.ShouldBeTrue();

        (await storage.UploadAsync("dir-2", options =>
        {
            options.Directory = "dir";
            options.FileName = "b.txt";
        })).IsSuccess.ShouldBeTrue();

        (await storage.UploadAsync("keep", options => options.FileName = "keep.txt")).IsSuccess.ShouldBeTrue();

        var deleteResult = await storage.DeleteDirectoryAsync("dir");
        deleteResult.IsSuccess.ShouldBeTrue();

        var dirAExists = await storage.ExistsAsync("dir/a.txt");
        dirAExists.IsSuccess.ShouldBeTrue();
        dirAExists.Value.ShouldBeFalse();

        var keepExists = await storage.ExistsAsync("keep.txt");
        keepExists.IsSuccess.ShouldBeTrue();
        keepExists.Value.ShouldBeTrue();

        var listed = new List<BlobMetadata>();
        await foreach (var item in storage.GetBlobMetadataListAsync("dir"))
        {
            listed.Add(item);
        }

        listed.ShouldBeEmpty();
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
            var fullPath = Combine(rootPath, path);
            var entry = _drive.Save(fullPath, content, contentType);
            return Task.FromResult(entry.ToDropboxFile(fullPath));
        }

        public Task<Stream> DownloadAsync(string rootPath, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Download(Combine(rootPath, path)));
        }

        public Task<bool> DeleteAsync(string rootPath, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Delete(Combine(rootPath, path)));
        }

        public Task<bool> ExistsAsync(string rootPath, string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(_drive.Exists(Combine(rootPath, path)));
        }

        public Task<DropboxItemMetadata?> GetMetadataAsync(string rootPath, string path, CancellationToken cancellationToken)
        {
            var fullPath = Combine(rootPath, path);
            return Task.FromResult<DropboxItemMetadata?>(_drive.Get(fullPath)?.ToDropboxFile(fullPath));
        }

        public async IAsyncEnumerable<DropboxItemMetadata> ListAsync(string rootPath, string? directory, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var fullPath = Combine(rootPath, directory ?? string.Empty);
            await foreach (var entry in _drive.List(fullPath, cancellationToken))
            {
                yield return entry.ToDropboxFile(entry.Path);
            }
        }

        private static string Normalize(string path)
        {
            var normalized = path.Replace("\\", "/");
            if (!normalized.StartsWith('/'))
            {
                normalized = "/" + normalized;
            }

            return normalized.TrimEnd('/') == string.Empty ? "/" : normalized.TrimEnd('/');
        }

        private static string Combine(string root, string path)
        {
            var normalizedRoot = Normalize(root);
            var normalizedPath = path.Replace("\\", "/").Trim('/');
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return normalizedRoot;
            }

            return normalizedRoot.EndsWith("/") ? normalizedRoot + normalizedPath : normalizedRoot + "/" + normalizedPath;
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
            var normalized = Normalize(path);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                var count = _entries.Count;
                _entries.Clear();
                return count > 0;
            }

            var keys = _entries.Keys
                .Where(key => key == normalized || key.StartsWith(normalized + "/"))
                .ToList();

            foreach (var key in keys)
            {
                _entries.Remove(key);
            }

            return keys.Count > 0;
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
                if (normalized == null
                    || string.Equals(entry.Path, normalized)
                    || entry.Path.StartsWith(normalized + "/"))
                {
                    yield return entry;
                }
            }
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
