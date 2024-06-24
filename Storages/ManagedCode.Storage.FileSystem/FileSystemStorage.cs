﻿using System;
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

namespace ManagedCode.Storage.FileSystem;

public class FileSystemStorage : BaseStorage<string, FileSystemStorageOptions>, IFileSystemStorage
{
    private readonly Dictionary<string, FileStream> _lockedFiles = new();

    public FileSystemStorage(FileSystemStorageOptions options) : base(options)
    {
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        if (Directory.Exists(StorageClient))
            Directory.Delete(StorageClient, true);

        return Result.Succeed();
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        var path = directory is null ? StorageClient : Path.Combine(StorageClient, directory);

        if (!Directory.Exists(path))
            yield break;

        foreach (var file in Directory.EnumerateFiles(path))
        {
            var blobMetadata = await GetBlobMetadataAsync(file, cancellationToken);

            if (blobMetadata.IsSuccess)
                yield return blobMetadata.Value!;
        }
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        var filePath = GetPathFromOptions(new DownloadOptions { FileName = fileName });

        return File.Exists(filePath)
            ? Result<Stream>.Succeed(new FileStream(filePath, FileMode.Open, FileAccess.Read))
            : Result<Stream>.Fail("File not found");
    }

    protected override string CreateStorageClient()
    {
        return StorageOptions.BaseFolder ?? Environment.CurrentDirectory;
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        if (!Directory.Exists(StorageClient))
            Directory.CreateDirectory(StorageClient);

        return Result.Succeed();
    }

    protected override Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(StorageClient, directory);
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        return Result.Succeed()
            .AsTask();
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        var filePath = GetPathFromOptions(options);

        using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
        {
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(fs, 81920, cancellationToken);
        }

        return await GetBlobMetadataInternalAsync(MetadataOptions.FromBaseOptions(options), cancellationToken);
    }

    private string GetPathFromOptions(BaseOptions options)
    {
        string filePath;

        if (options.Directory is not null)
        {
            EnsureDirectoryExist(options.Directory);
            filePath = Path.Combine(StorageClient, options.Directory, options.FileName);
        }
        else
        {
            filePath = Path.Combine(StorageClient, options.FileName);
        }

        EnsureDirectoryExist(Path.GetDirectoryName(filePath));

        return filePath;
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        var filePath = GetPathFromOptions(options);

        return File.Exists(filePath) ? Result<LocalFile>.Succeed(new LocalFile(filePath)) : Result<LocalFile>.Fail("File not found");
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();

        var filePath = GetPathFromOptions(options);

        if (!File.Exists(filePath))
            return Result<bool>.Succeed(false);

        File.Delete(filePath);
        return Result<bool>.Succeed(true);
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        var filePath = GetPathFromOptions(options);
        return Result<bool>.Succeed(File.Exists(filePath));
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist();
        var filePath = GetPathFromOptions(options);
        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
            return Result<BlobMetadata>.Fail("File not found");

        var result = new BlobMetadata
        {
            Name = fileInfo.Name,
            Uri = new Uri(Path.Combine(StorageClient, filePath)),
            MimeType = MimeHelper.GetMimeType(fileInfo.Extension),
            CreatedOn = fileInfo.CreationTimeUtc,
            LastModified = fileInfo.LastWriteTimeUtc,
            Length = fileInfo.Length
        };

        return Result<BlobMetadata>.Succeed(result);
    }

    protected override async Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetPathFromOptions(options);

        await EnsureContainerExist();
        if (hasLegalHold && !_lockedFiles.ContainsKey(filePath))
        {
            var file = await DownloadAsync(filePath, cancellationToken);

            if (file.IsFailed)
                return Result.Fail(file.Errors);

            var fileStream = File.OpenRead(file.Value!.FilePath); // Opening with FileAccess.Read only
            if (Environment.OSVersion.Platform != PlatformID.MacOSX)
                fileStream.Lock(0, fileStream.Length); // Attempting to lock a region of the read-only file

            _lockedFiles.Add(filePath, fileStream);

            return Result.Succeed();
        }

        if (!hasLegalHold)
            if (_lockedFiles.ContainsKey(filePath))
            {
                _lockedFiles[filePath]
                    .Unlock(0, _lockedFiles[filePath].Length);
                _lockedFiles[filePath]
                    .Dispose();
                _lockedFiles.Remove(filePath);
            }

        return Result.Succeed();
    }

    protected override async Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        var filePath = GetPathFromOptions(options);
        await EnsureContainerExist();
        return Result<bool>.Succeed(_lockedFiles.ContainsKey(filePath));
    }

    private void EnsureDirectoryExist(string directory)
    {
        var path = Path.Combine(StorageClient, directory);

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}