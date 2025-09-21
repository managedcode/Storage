using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Communication;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Sftp.Options;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace ManagedCode.Storage.Sftp;

/// <summary>
/// SFTP storage implementation backed by SSH.NET.
/// </summary>
public class SftpStorage : BaseStorage<SftpClient, SftpStorageOptions>, ISftpStorage
{
    private readonly ILogger<SftpStorage> _logger;

    public SftpStorage(SftpStorageOptions options, ILogger<SftpStorage> logger)
        : base(options)
    {
        _logger = logger;
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();

            var root = NormalizeRemotePath(StorageOptions.RemoteDirectory);
            if (string.IsNullOrEmpty(root) || root == "/")
            {
                // Do not delete the root directory; cleanup files instead
                await DeleteDirectoryContentsAsync(root, cancellationToken);
            }
            else if (StorageClient.Exists(root))
            {
                await DeleteDirectoryRecursiveAsync(root, cancellationToken);
            }

            IsContainerCreated = false;
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove SFTP container {Directory}", StorageOptions.RemoteDirectory);
            return Result.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist(cancellationToken);
        EnsureConnected();

        var path = NormalizeRemotePath(directory ?? string.Empty, allowEmpty: true);
        var isRoot = string.IsNullOrEmpty(path);
        var targetPath = isRoot ? CurrentRoot : path;

        if (!StorageClient.Exists(targetPath))
        {
            yield break;
        }

        var listing = StorageClient.ListDirectory(targetPath);

        foreach (var item in listing)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (item.Name == "." || item.Name == ".." || item.IsDirectory)
                continue;

            var metadata = MapToBlobMetadata(item);
            yield return metadata;
        }
    }

    public async Task<Result<Stream>> OpenReadStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            EnsureConnected();

            var remotePath = BuildPath(fileName);
            if (!StorageClient.Exists(remotePath))
            {
                return Result<Stream>.Fail($"File not found: {fileName}");
            }

            var stream = StorageClient.OpenRead(remotePath);
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open read stream for {File}", fileName);
            return Result<Stream>.Fail(ex);
        }
    }

    public override Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return OpenReadStreamAsync(fileName, cancellationToken);
    }

    public async Task<Result<Stream>> OpenWriteStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            EnsureConnected();

            var remotePath = BuildPath(fileName);
            EnsureRemoteDirectory(PathHelper.GetUnixDirectoryPath(remotePath));
            var stream = StorageClient.OpenWrite(remotePath);
            return Result<Stream>.Succeed(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open write stream for {File}", fileName);
            return Result<Stream>.Fail(ex);
        }
    }

    public async Task<Result<bool>> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            await Task.CompletedTask;
            return Result<bool>.Succeed(StorageClient.IsConnected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test SFTP connection");
            return Result<bool>.Fail(ex);
        }
    }

    public async Task<Result<string>> GetWorkingDirectoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            await Task.CompletedTask;
            return Result<string>.Succeed(StorageClient.WorkingDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SFTP working directory");
            return Result<string>.Fail(ex);
        }
    }

    public async Task<Result> ChangeWorkingDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            StorageClient.ChangeDirectory(NormalizeRemotePath(directory));
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change SFTP working directory to {Directory}", directory);
            return Result.Fail(ex);
        }
    }

    protected override SftpClient CreateStorageClient()
    {
        if (string.IsNullOrEmpty(StorageOptions.Host))
            throw new ArgumentException("Host must be specified", nameof(StorageOptions.Host));
        if (string.IsNullOrEmpty(StorageOptions.Username))
            throw new ArgumentException("Username must be specified", nameof(StorageOptions.Username));

        var authMethods = new List<AuthenticationMethod>();

        if (!string.IsNullOrEmpty(StorageOptions.Password))
        {
            authMethods.Add(new PasswordAuthenticationMethod(StorageOptions.Username, StorageOptions.Password));
        }

        if (!string.IsNullOrEmpty(StorageOptions.PrivateKeyContent) || !string.IsNullOrEmpty(StorageOptions.PrivateKeyPath))
        {
            using Stream privateKeyStream = !string.IsNullOrEmpty(StorageOptions.PrivateKeyContent)
                ? new MemoryStream(Encoding.UTF8.GetBytes(StorageOptions.PrivateKeyContent))
                : File.OpenRead(StorageOptions.PrivateKeyPath!);

            var keyFile = string.IsNullOrEmpty(StorageOptions.PrivateKeyPassphrase)
                ? new PrivateKeyFile(privateKeyStream)
                : new PrivateKeyFile(privateKeyStream, StorageOptions.PrivateKeyPassphrase);

            authMethods.Add(new PrivateKeyAuthenticationMethod(StorageOptions.Username, keyFile));
        }

        if (!authMethods.Any())
        {
            throw new ArgumentException("SFTP requires at least one authentication method (password or private key)");
        }

        var connectionInfo = new ConnectionInfo(
            StorageOptions.Host,
            StorageOptions.Port,
            StorageOptions.Username,
            authMethods.ToArray())
        {
            Timeout = TimeSpan.FromMilliseconds(StorageOptions.ConnectTimeout)
        };

        var client = new SftpClient(connectionInfo)
        {
            OperationTimeout = TimeSpan.FromMilliseconds(StorageOptions.OperationTimeout)
        };

        if (StorageOptions.AcceptAnyHostKey)
        {
            client.HostKeyReceived += (_, args) => args.CanTrust = true;
        }
        else if (!string.IsNullOrEmpty(StorageOptions.HostKeyFingerprint))
        {
            client.HostKeyReceived += (_, args) =>
            {
                var fingerprint = BitConverter.ToString(args.FingerPrint).Replace('-', ':');
                args.CanTrust = string.Equals(fingerprint, StorageOptions.HostKeyFingerprint, StringComparison.OrdinalIgnoreCase);
            };
        }

        return client;
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();

            var root = NormalizeRemotePath(StorageOptions.RemoteDirectory);
            if (!StorageClient.Exists(root))
            {
                StorageClient.CreateDirectory(root);
            }

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SFTP container {Directory}", StorageOptions.RemoteDirectory);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            var path = BuildPath(directory);

            if (StorageClient.Exists(path))
            {
                await DeleteDirectoryRecursiveAsync(path, cancellationToken);
            }

            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete SFTP directory {Directory}", directory);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> UploadInternalAsync(Stream stream, UploadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            EnsureConnected();

            var remotePath = BuildPath(options.FullPath);
            EnsureRemoteDirectory(PathHelper.GetUnixDirectoryPath(remotePath));

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                StorageClient.UploadFile(stream, remotePath, true);
            }, cancellationToken);

            var metadataOptions = MetadataOptions.FromBaseOptions(options);
            return await GetBlobMetadataInternalAsync(metadataOptions, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "SFTP upload cancelled for {File}", options.FullPath);
            return Result<BlobMetadata>.Fail(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload SFTP file {File}", options.FullPath);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result<LocalFile>> DownloadInternalAsync(LocalFile localFile, DownloadOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            EnsureConnected();

            var remotePath = BuildPath(options.FullPath);
            if (!StorageClient.Exists(remotePath))
            {
                return Result<LocalFile>.Fail("File not found");
            }

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var destinationStream = localFile.FileStream;
                destinationStream.SetLength(0);
                StorageClient.DownloadFile(remotePath, destinationStream);
            }, cancellationToken);

            return Result<LocalFile>.Succeed(localFile);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "SFTP download cancelled for {File}", options.FullPath);
            return Result<LocalFile>.Fail(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download SFTP file {File}", options.FullPath);
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            EnsureConnected();

            var remotePath = BuildPath(options.FullPath);
            if (!StorageClient.Exists(remotePath))
            {
                return Result<bool>.Succeed(false);
            }

            StorageClient.DeleteFile(remotePath);
            return Result<bool>.Succeed(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete SFTP file {File}", options.FullPath);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            EnsureConnected();

            var remotePath = BuildPath(options.FullPath);
            var exists = StorageClient.Exists(remotePath);
            return Result<bool>.Succeed(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check SFTP existence for {File}", options.FullPath);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            EnsureConnected();

            var remotePath = BuildPath(options.FullPath);
            if (!StorageClient.Exists(remotePath))
            {
                return Result<BlobMetadata>.Fail("File not found");
            }

            var attributes = StorageClient.GetAttributes(remotePath);
            var metadata = new BlobMetadata
            {
                FullName = NormalizeRelativeName(remotePath),
                Name = Path.GetFileName(remotePath),
                Uri = BuildUri(remotePath),
                Container = StorageOptions.RemoteDirectory,
                Length = (ulong)attributes.Size,
                CreatedOn = attributes.LastWriteTimeUtc,
                LastModified = attributes.LastWriteTimeUtc,
                MimeType = MimeHelper.GetMimeType(Path.GetExtension(remotePath)),
                Metadata = new Dictionary<string, string>()
            };

            return Result<BlobMetadata>.Succeed(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SFTP metadata for {File}", options.FullPath);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        // Not supported for SFTP
        return Task.FromResult(Result.Succeed());
    }

    protected override Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        // Not supported for SFTP
        return Task.FromResult(Result<bool>.Succeed(false));
    }

    private string CurrentRoot => NormalizeRemotePath(StorageOptions.RemoteDirectory, allowEmpty: false);

    private void EnsureConnected()
    {
        if (!StorageClient.IsConnected)
        {
            StorageClient.Connect();
        }

        try
        {
            StorageClient.ChangeDirectory(CurrentRoot);
        }
        catch (SftpPathNotFoundException)
        {
            if (StorageOptions.CreateContainerIfNotExists && !StorageClient.Exists(CurrentRoot))
            {
                StorageClient.CreateDirectory(CurrentRoot);
                StorageClient.ChangeDirectory(CurrentRoot);
            }
            else
            {
                throw;
            }
        }
    }

    private void EnsureRemoteDirectory(string? directory)
    {
        if (string.IsNullOrEmpty(directory) || directory == "/")
            return;

        var segments = directory.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var path = directory.StartsWith('/') ? "/" : CurrentRoot;

        foreach (var segment in segments)
        {
            path = path == "/" ? $"/{segment}" : $"{path}/{segment}";
            if (!StorageClient.Exists(path))
            {
                StorageClient.CreateDirectory(path);
            }
        }
    }

    private string BuildPath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return CurrentRoot;
        }

        var normalizedFileName = fileName.Replace('\\', '/');
        if (normalizedFileName.StartsWith('/'))
        {
            return normalizedFileName;
        }

        return CurrentRoot.EndsWith("/")
            ? CurrentRoot + normalizedFileName
            : $"{CurrentRoot}/{normalizedFileName}";
    }

    private string NormalizeRemotePath(string? path, bool allowEmpty = false)
    {
        if (string.IsNullOrEmpty(path))
            return allowEmpty ? string.Empty : "/";

        path = path.Replace('\\', '/');
        if (!path.StartsWith('/'))
        {
            path = $"/{path}";
        }

        path = PathHelper.ToUnixPath(path);
        return PathHelper.EnsureAbsolutePath(path, '/');
    }

    private string NormalizeRelativeName(string remotePath)
    {
        if (string.IsNullOrEmpty(StorageOptions.RemoteDirectory) || StorageOptions.RemoteDirectory == "/")
        {
            return remotePath.TrimStart('/');
        }

        var root = NormalizeRemotePath(StorageOptions.RemoteDirectory);
        if (remotePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return remotePath[root.Length..].TrimStart('/');
        }

        return remotePath.TrimStart('/');
    }

    private Uri? BuildUri(string remotePath)
    {
        if (string.IsNullOrEmpty(StorageOptions.Host))
        {
            return null;
        }

        var builder = new UriBuilder("sftp", StorageOptions.Host, StorageOptions.Port)
        {
            Path = remotePath
        };

        return builder.Uri;
    }

    private BlobMetadata MapToBlobMetadata(ISftpFile file)
    {
        return new BlobMetadata
        {
            FullName = NormalizeRelativeName(file.FullName),
            Name = file.Name,
            Uri = BuildUri(file.FullName),
            Container = StorageOptions.RemoteDirectory,
            Length = (ulong)file.Attributes.Size,
            CreatedOn = file.Attributes.LastWriteTimeUtc,
            LastModified = file.Attributes.LastWriteTimeUtc,
            MimeType = MimeHelper.GetMimeType(Path.GetExtension(file.Name)),
            Metadata = new Dictionary<string, string>()
        };
    }

    private Task DeleteDirectoryRecursiveAsync(string path, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            DeleteDirectoryRecursive(path, cancellationToken);
        }, cancellationToken);
    }

    private void DeleteDirectoryRecursive(string path, CancellationToken cancellationToken)
    {
        foreach (var entry in StorageClient.ListDirectory(path))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Name == "." || entry.Name == "..")
                continue;

            if (entry.IsDirectory)
            {
                DeleteDirectoryRecursive(entry.FullName, cancellationToken);
                StorageClient.DeleteDirectory(entry.FullName);
            }
            else
            {
                StorageClient.DeleteFile(entry.FullName);
            }
        }

        if (!string.Equals(path, CurrentRoot, StringComparison.Ordinal))
        {
            StorageClient.DeleteDirectory(path);
        }
    }

    private Task DeleteDirectoryContentsAsync(string path, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var target = string.IsNullOrEmpty(path) ? CurrentRoot : path;
            foreach (var entry in StorageClient.ListDirectory(target))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.Name == "." || entry.Name == "..")
                    continue;

                if (entry.IsDirectory)
                {
                    DeleteDirectoryRecursive(entry.FullName, cancellationToken);
                    StorageClient.DeleteDirectory(entry.FullName);
                }
                else
                {
                    StorageClient.DeleteFile(entry.FullName);
                }
            }
        }, cancellationToken);
    }
}
