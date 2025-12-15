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

            var relativePath = Path.GetRelativePath(StorageClient, file)
                .Replace('\\', '/');

            var (relativeDirectory, relativeFileName) = SplitRelativePath(relativePath);
            MetadataOptions options = new()
            {
                FileName = relativeFileName,
                Directory = relativeDirectory
            };

            var blobMetadata = await GetBlobMetadataAsync(options, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (blobMetadata.IsSuccess)
            {
                yield return blobMetadata.Value;
                continue;
            }
        }
    }

    private static (string? Directory, string FileName) SplitRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Relative path cannot be null or empty", nameof(relativePath));

        var normalizedPath = relativePath.Replace('\\', '/');
        var separatorIndex = normalizedPath.LastIndexOf('/');

        if (separatorIndex < 0)
            return (null, normalizedPath);

        var directory = separatorIndex == 0
            ? null
            : normalizedPath[..separatorIndex];

        var fileName = normalizedPath[(separatorIndex + 1)..];

        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException($"Invalid relative path: '{relativePath}'");

        return (string.IsNullOrWhiteSpace(directory) ? null : directory, fileName);
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

            var sourcePath = GetPathFromOptions(options);
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(sourcePath))
            {
                return Result<LocalFile>.Fail("File not found");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var metadata = await GetBlobMetadataInternalAsync(MetadataOptions.FromBaseOptions(options), cancellationToken);

            if (options.LocalPath is null)
            {
                var linkedFile = new LocalFile(sourcePath, keepAlive: true);
                if (metadata.IsSuccess)
                {
                    linkedFile.BlobMetadata = metadata.Value;
                }

                return Result<LocalFile>.Succeed(linkedFile);
            }

            File.Copy(sourcePath, localFile.FilePath, overwrite: true);
            if (metadata.IsSuccess)
            {
                localFile.BlobMetadata = metadata.Value;
            }

            return Result<LocalFile>.Succeed(localFile);
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
        if (string.IsNullOrWhiteSpace(options.FileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(options.FileName));

        var (directoryFromFileName, fileNameOnly) = SplitDirectoryFromFileName(options.FileName);

        // Sanitize and validate components
        var sanitizedFileName = SanitizeFileName(fileNameOnly);

        var combinedDirectory = CombineDirectoryParts(options.Directory, directoryFromFileName);
        var sanitizedDirectory = combinedDirectory is not null
            ? SanitizeDirectory(combinedDirectory)
            : null;

        if (sanitizedDirectory is not null)
        {
            EnsureDirectoryExist(sanitizedDirectory);
        }

        string filePath = sanitizedDirectory is not null
            ? Path.Combine(StorageClient, sanitizedDirectory, sanitizedFileName)
            : Path.Combine(StorageClient, sanitizedFileName);

        // Get full paths for comparison
        var fullPath = Path.GetFullPath(filePath);
        var baseFullPath = Path.GetFullPath(StorageClient);

        // Verify the final path is within StorageClient directory
        if (!fullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Access to path '{options.FileName}' is denied. Path traversal detected.");
        }

        EnsureDirectoryExist(Path.GetDirectoryName(fullPath)!);
        return fullPath;
    }

    private static (string? Directory, string FileName) SplitDirectoryFromFileName(string fileName)
    {
        var normalized = fileName.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');

        if (lastSlash < 0)
            return (null, normalized);

        var directory = normalized[..lastSlash];
        var name = normalized[(lastSlash + 1)..];
        return (directory, name);
    }

    private static string? CombineDirectoryParts(string? primary, string? secondary)
    {
        var parts = new List<string>();

        void AddPart(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var normalized = value.Replace('\\', '/');
            foreach (var segment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                parts.Add(segment);
            }
        }

        AddPart(primary);
        AddPart(secondary);

        if (parts.Count == 0)
            return null;

        return string.Join('/', parts);
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

        var originalFileName = fileName;

        // Check for path traversal attempts - throw exception if detected
        if (fileName.Contains("..", StringComparison.Ordinal))
            throw new UnauthorizedAccessException($"Access to path '{originalFileName}' is denied. Path traversal detected.");

        // If there are path separators, extract only the filename part
        // This handles cases like /tmp/file.txt -> file.txt
        if (fileName.Contains('/') || fileName.Contains('\\'))
        {
            fileName = Path.GetFileName(fileName);
        }

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Invalid file name", nameof(fileName));

        // Remove any invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = fileName;
        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c.ToString(), string.Empty);
        }

        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("File name contains only invalid characters", nameof(fileName));

        return sanitized;
    }

    private static string SanitizeDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return string.Empty;

        var originalDirectory = directory;

        // Check for path traversal attempts - throw exception if detected
        if (directory.Contains("..", StringComparison.Ordinal))
            throw new UnauthorizedAccessException($"Access to path '{originalDirectory}' is denied. Path traversal detected.");

        // Normalize path separators
        directory = directory.Replace('\\', '/');

        // Remove leading and trailing slashes
        directory = directory.Trim('/');

        // Validate each directory segment
        var segments = directory.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var invalidChars = Path.GetInvalidFileNameChars();

        foreach (var segment in segments)
        {
            if (segment.IndexOfAny(invalidChars) >= 0)
                throw new ArgumentException($"Directory path contains invalid characters: {segment}", nameof(directory));

            // Additional check for suspicious segments
            if (segment == ".." || segment == ".")
                throw new UnauthorizedAccessException($"Access to path '{originalDirectory}' is denied. Path traversal detected.");
        }

        return string.Join(Path.DirectorySeparatorChar, segments);
    }

    private void EnsureDirectoryExist(string directory)
    {
        var path = Path.Combine(StorageClient, directory);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
