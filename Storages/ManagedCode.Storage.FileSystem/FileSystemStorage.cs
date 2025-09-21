using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.FileSystem.Options;

namespace ManagedCode.Storage.FileSystem;

public class FileSystemStorage(FileSystemStorageOptions options) : BaseStorage<string, FileSystemStorageOptions>(options), IFileSystemStorage
{
    private readonly Dictionary<string, FileStream> _lockedFiles = new();

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (Directory.Exists(StorageClient))
                Directory.Delete(StorageClient, true);

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist(cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
            yield break;

        var searchRoot = string.IsNullOrEmpty(directory)
            ? StorageClient
            : Path.Combine(StorageClient, directory!);

        if (!Directory.Exists(searchRoot))
            yield break;

        foreach (var file in Directory.EnumerateFiles(searchRoot, "*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            var blobMetadata = await GetBlobMetadataAsync(file, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (blobMetadata.IsSuccess)
                yield return blobMetadata.Value;
        }
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = GetPathFromOptions(new DownloadOptions { FileName = fileName });
            cancellationToken.ThrowIfCancellationRequested();

            return File.Exists(filePath)
                ? Result<Stream>.Succeed(new FileStream(filePath, FileMode.Open, FileAccess.Read))
                : Result<Stream>.Fail("File not found");
        }
        catch (Exception ex)
        {
            return Result<Stream>.Fail(ex);
        }
    }

    protected override string CreateStorageClient()
    {
        return StorageOptions.BaseFolder ?? Environment.CurrentDirectory;
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(StorageClient))
                Directory.CreateDirectory(StorageClient);

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(StorageClient, directory);

            if (Directory.Exists(path))
                Directory.Delete(path, true);

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = GetPathFromOptions(options);
            cancellationToken.ThrowIfCancellationRequested();

            const int bufferSize = 4096 * 1024; // 4MB buffer
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            await stream.CopyToAsync(fileStream, bufferSize, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            return await GetBlobMetadataInternalAsync(MetadataOptions.FromBaseOptions(options), cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = GetPathFromOptions(options);
            cancellationToken.ThrowIfCancellationRequested();

            return File.Exists(filePath)
                ? Result<LocalFile>.Succeed(new LocalFile(filePath))
                : Result<LocalFile>.Fail("File not found");
        }
        catch (Exception ex)
        {
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = GetPathFromOptions(options);
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(filePath))
                return Result<bool>.Succeed(false);

            File.Delete(filePath);
            return Result<bool>.Succeed(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = GetPathFromOptions(options);
            cancellationToken.ThrowIfCancellationRequested();

            return Result<bool>.Succeed(File.Exists(filePath));
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var filePath = GetPathFromOptions(options);
            cancellationToken.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return Result<BlobMetadata>.Fail("File not found");

            var relativePath = Path.GetRelativePath(StorageClient, filePath)
                .Replace('\\', '/');

            var result = new BlobMetadata
            {
                FullName = relativePath,
                Name = fileInfo.Name,
                Uri = new Uri(Path.Combine(StorageClient, filePath)),
                MimeType = MimeHelper.GetMimeType(fileInfo.Extension),
                CreatedOn = fileInfo.CreationTimeUtc,
                LastModified = fileInfo.LastWriteTimeUtc,
                Length = (ulong)fileInfo.Length
            };

            return Result<BlobMetadata>.Succeed(result);
        }
        catch (Exception ex)
        {
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetPathFromOptions(options);
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (hasLegalHold && !_lockedFiles.ContainsKey(filePath))
            {
                var file = await DownloadAsync(filePath, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (file.IsFailed)
                    return Result.Fail(file.Problem);

                var fileStream = File.OpenRead(file.Value!.FilePath);
                if (Environment.OSVersion.Platform != PlatformID.MacOSX)
                    fileStream.Lock(0, fileStream.Length);

                _lockedFiles.Add(filePath, fileStream);
            }
            else if (!hasLegalHold && _lockedFiles.ContainsKey(filePath))
            {
                _lockedFiles[filePath].Unlock(0, _lockedFiles[filePath].Length);
                _lockedFiles[filePath].Dispose();
                _lockedFiles.Remove(filePath);
            }

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetPathFromOptions(options);
            await EnsureContainerExist(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            return Result<bool>.Succeed(_lockedFiles.ContainsKey(filePath));
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex);
        }
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

        EnsureDirectoryExist(Path.GetDirectoryName(filePath)!);
        return filePath;
    }

    private void EnsureDirectoryExist(string directory)
    {
        var path = Path.Combine(StorageClient, directory);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
