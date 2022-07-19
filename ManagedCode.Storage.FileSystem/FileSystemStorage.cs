using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem.Options;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Storage.FileSystem;

public class FileSystemStorage : BaseStorage<FileSystemStorageOptions>, IFileSystemStorage
{
    private readonly string _path;
    private readonly Dictionary<string, FileStream> _lockedFiles = new();

    public FileSystemStorage(ILogger<FileSystemStorage> logger, FileSystemStorageOptions options) : base(options)
    {
        _path = StorageOptions.BaseFolder ?? Environment.CurrentDirectory;
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        if (!Directory.Exists(_path))
        {
            Directory.CreateDirectory(_path);
        }

        return Result.Succeeded();
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        if (Directory.Exists(_path))
        {
            Directory.Delete(_path);
        }

        return Result.Succeeded();
    }

    protected override async Task<Result<string>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        var filePath = Path.Combine(_path, options.FileName);

        using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
        {
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(fs, 81920, cancellationToken);
        }

        return Result<string>.Succeeded(filePath);
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, string blob,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        var filePath = Path.Combine(_path, blob);

        if (File.Exists(filePath))
        {
            return Result<LocalFile>.Succeeded(new LocalFile(filePath));
        }

        return Result<LocalFile>.Failed();
    }

    public override async Task<Result<bool>> DeleteAsync(string blob, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        var filePath = Path.Combine(_path, blob);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return Result<bool>.Succeeded(true);
        }

        return Result<bool>.Succeeded(false);
    }

    public override async Task<Result<bool>> ExistsAsync(string blob, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        var filePath = Path.Combine(_path, blob);
        return Result<bool>.Succeeded(File.Exists(filePath));
    }

    public override async Task<Result<BlobMetadata>> GetBlobMetadataAsync(string blob, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        var fileInfo = new FileInfo(Path.Combine(_path, blob));
        if (fileInfo.Exists)
        {
            var result = new BlobMetadata
            {
                Name = fileInfo.Name,
                Uri = new Uri(Path.Combine(_path, blob)),
                MimeType = MimeHelper.GetMimeType(fileInfo.Extension),
                Length = fileInfo.Length
            };

            return Result<BlobMetadata>.Succeeded(result);
        }

        return Result<BlobMetadata>.Failed();
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        foreach (var file in Directory.EnumerateFiles(_path))
        {
            var blobMetadata = await GetBlobMetadataAsync(file, cancellationToken);

            if (blobMetadata.IsSucceeded)
            {
                yield return blobMetadata.Value!;
            }
        }
    }

    public override async Task<Result> SetLegalHoldAsync(string blob, bool hasLegalHold, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        if (hasLegalHold && !_lockedFiles.ContainsKey(blob))
        {
            var file = await DownloadAsync(blob, cancellationToken);

            if (file.IsError)
                return Result.Failed();

            var fileStream = File.OpenRead(file.Value!.FilePath); // Opening with FileAccess.Read only
            fileStream.Lock(0, fileStream.Length); // Attempting to lock a region of the read-only file

            _lockedFiles.Add(blob, fileStream);

            return Result.Succeeded();
        }

        if (!hasLegalHold)
        {
            if (_lockedFiles.ContainsKey(blob))
            {
                _lockedFiles[blob].Unlock(0, _lockedFiles[blob].Length);
                _lockedFiles[blob].Dispose();
                _lockedFiles.Remove(blob);
            }
        }

        return Result.Succeeded();
    }

    public override async Task<Result<bool>> HasLegalHoldAsync(string blob, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        return Result<bool>.Succeeded(_lockedFiles.ContainsKey(blob));
    }
}