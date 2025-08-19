using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Ftp.Options;
using Microsoft.Extensions.Logging;
using ManagedCode.Storage.Core.Constants;
using ManagedCode.Storage.Core.Helpers;

namespace ManagedCode.Storage.Ftp;

/// <summary>
/// FTP storage implementation supporting FTP, FTPS, and SFTP protocols.
/// </summary>
public class FtpStorage : BaseStorage<FtpClient, IFtpStorageOptions>, IFtpStorage
{
    private readonly ILogger<FtpStorage> _logger;

    public FtpStorage(IFtpStorageOptions options, ILogger<FtpStorage> logger)
        : base(options)
    {
        _logger = logger;
    }

    public override async Task<Result> RemoveContainerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            
            if (!string.IsNullOrEmpty(StorageOptions.RemoteDirectory) && 
                StorageOptions.RemoteDirectory != "/")
            {
                var exists = StorageClient.DirectoryExists(StorageOptions.RemoteDirectory);
                if (exists)
                {
                    StorageClient.DeleteDirectory(StorageOptions.RemoteDirectory);
                }
            }
            
            IsContainerCreated = false;
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove container: {Directory}", StorageOptions.RemoteDirectory);
            return Result.Fail(ex);
        }
    }

    public override async IAsyncEnumerable<BlobMetadata> GetBlobMetadataListAsync(
        string? directory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureContainerExist(cancellationToken);
        EnsureConnected();

        var searchPath = BuildPath(directory ?? string.Empty);
        
        var items = StorageClient.GetListing(searchPath);
        
        foreach (var item in items.Where(x => x.Type == FtpObjectType.File))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var relativePath = GetRelativePath(item.FullName);
            
            var blobMetadata = new BlobMetadata
            {
                FullName = relativePath,
                Name = item.Name,
                Uri = BuildUri(relativePath),
                Container = StorageOptions.RemoteDirectory,
                Length = (ulong)Math.Max(0, item.Size),
                LastModified = item.Modified,
                CreatedOn = item.Created != DateTime.MinValue ? item.Created : item.Modified,
                MimeType = GetMimeType(item.Name),
                Metadata = new Dictionary<string, string>
                {
                    [MetadataKeys.FtpRawPermissions] = item.RawPermissions ?? string.Empty,
                    [MetadataKeys.FtpFileType] = item.Type switch
                    {
                        FtpObjectType.File => MetadataValues.FileTypes.File,
                        FtpObjectType.Directory => MetadataValues.FileTypes.Directory,
                        FtpObjectType.Link => MetadataValues.FileTypes.SymbolicLink,
                        _ => MetadataValues.FileTypes.Unknown
                    }
                }
            };

            yield return blobMetadata;
        }
    }

    public async Task<Result<Stream>> OpenReadStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            EnsureConnected();
            
            var remotePath = BuildPath(fileName);
            
            // First verify the file exists
            if (!StorageClient.FileExists(remotePath))
            {
                return Result<Stream>.Fail($"File not found: {fileName}");
            }
            
            // Try different approaches for opening read stream
            try
            {
                // First try with passive mode
                var originalDataConnectionType = StorageClient.Config.DataConnectionType;
                StorageClient.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                var stream = StorageClient.OpenRead(remotePath, FtpDataType.Binary);
                return Result<Stream>.Succeed(stream);
            }
            catch (Exception passiveEx)
            {
                _logger?.LogWarning(passiveEx, "Passive mode failed for OpenRead, trying active mode");
                
                try
                {
                    // Try with active mode
                    StorageClient.Config.DataConnectionType = FtpDataConnectionType.AutoActive;
                    var stream = StorageClient.OpenRead(remotePath, FtpDataType.Binary);
                    return Result<Stream>.Succeed(stream);
                }
                catch (Exception activeEx)
                {
                    _logger?.LogWarning(activeEx, "Active mode also failed for OpenRead, using download approach");
                    
                    // Fallback: download to memory stream
                    var memoryStream = new MemoryStream();
                    var success = StorageClient.DownloadStream(memoryStream, remotePath);
                    if (success)
                    {
                        memoryStream.Position = 0;
                        return Result<Stream>.Succeed(memoryStream);
                    }
                    
                    throw activeEx;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open read stream for file: {FileName}", fileName);
            return Result<Stream>.Fail(ex);
        }
    }

    public async Task<Result<Stream>> OpenWriteStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureContainerExist(cancellationToken);
            EnsureConnected();
            
            var remotePath = BuildPath(fileName);
            
            // Ensure directory exists
            var directoryPath = PathHelper.GetUnixDirectoryPath(remotePath);
            if (!string.IsNullOrEmpty(directoryPath) && directoryPath != "/")
            {
                StorageClient.CreateDirectory(directoryPath, true);
            }
            
            // Try different approaches for opening write stream
            try
            {
                // First try with passive mode
                var originalDataConnectionType = StorageClient.Config.DataConnectionType;
                StorageClient.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                var stream = StorageClient.OpenWrite(remotePath, FtpDataType.Binary);
                
                // Wrap the stream to ensure proper disposal and persistence
                return Result<Stream>.Succeed(new FtpWriteStreamWrapper(stream, StorageClient, remotePath, _logger));
            }
            catch (Exception passiveEx)
            {
                _logger?.LogWarning(passiveEx, "Passive mode failed for OpenWrite, trying active mode");
                
                try
                {
                    // Try with active mode
                    StorageClient.Config.DataConnectionType = FtpDataConnectionType.AutoActive;
                    var stream = StorageClient.OpenWrite(remotePath, FtpDataType.Binary);
                    
                    // Wrap the stream to ensure proper disposal and persistence
                    return Result<Stream>.Succeed(new FtpWriteStreamWrapper(stream, StorageClient, remotePath, _logger));
                }
                catch (Exception activeEx)
                {
                    _logger?.LogWarning(activeEx, "Active mode also failed for OpenWrite, using memory stream approach");
                    
                    // Fallback: use memory stream that uploads on dispose
                    var memoryStream = new FtpMemoryWriteStream(StorageClient, remotePath, _logger);
                    return Result<Stream>.Succeed(memoryStream);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open write stream for file: {FileName}", fileName);
            return Result<Stream>.Fail(ex);
        }
    }

    public async Task<Result<bool>> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            return Result<bool>.Succeed(StorageClient.IsConnected);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Connection test failed");
            return Result<bool>.Fail(ex);
        }
    }

    public async Task<Result<string>> GetWorkingDirectoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            var workingDirectory = StorageClient.GetWorkingDirectory();
            return Result<string>.Succeed(workingDirectory);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get working directory");
            return Result<string>.Fail(ex);
        }
    }

    public async Task<Result> ChangeWorkingDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            StorageClient.SetWorkingDirectory(directory);
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to change working directory to: {Directory}", directory);
            return Result.Fail(ex);
        }
    }

    public override async Task<Result<Stream>> GetStreamAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return await OpenReadStreamAsync(fileName, cancellationToken);
    }

    protected override FtpClient CreateStorageClient()
    {
        var client = new FtpClient();
        ConfigureFtpClient(client);
        return client;
    }

    protected override async Task<Result> CreateContainerInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            
            if (StorageOptions.CreateContainerIfNotExists && 
                !string.IsNullOrEmpty(StorageOptions.RemoteDirectory) && 
                StorageOptions.RemoteDirectory != "/")
            {
                var exists = StorageClient.DirectoryExists(StorageOptions.RemoteDirectory);
                if (!exists)
                {
                    StorageClient.CreateDirectory(StorageOptions.RemoteDirectory, true);
                }
            }
            
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create container: {Directory}", StorageOptions.RemoteDirectory);
            return Result.Fail(ex);
        }
    }

    protected override async Task<Result> DeleteDirectoryInternalAsync(string directory, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            
            var remotePath = BuildPath(directory);
            var exists = StorageClient.DirectoryExists(remotePath);
            
            if (exists)
            {
                StorageClient.DeleteDirectory(remotePath);
            }
            
            return Result.Succeed();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete directory: {Directory}", directory);
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
            
            // Check cancellation before starting upload
            cancellationToken.ThrowIfCancellationRequested();
            
            var remotePath = BuildPath(options.FullPath);
            
            // Ensure directory exists
            var directoryPath = PathHelper.GetUnixDirectoryPath(remotePath);
            if (!string.IsNullOrEmpty(directoryPath) && directoryPath != "/")
            {
                StorageClient.CreateDirectory(directoryPath, true);
            }
            
            // Check cancellation again before upload
            cancellationToken.ThrowIfCancellationRequested();
            
            FtpStatus result;
            
            // Use Task.Run to make synchronous method respect cancellation
            try
            {
                result = await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return StorageClient.UploadStream(stream, remotePath, FtpRemoteExists.Overwrite, false);
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("Upload operation was cancelled for file: {FileName}", options.FullPath);
                throw;
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                _logger?.LogInformation("Upload operation was cancelled for file: {FileName}", options.FullPath);
                throw new OperationCanceledException("Upload was cancelled", ex, cancellationToken);
            }
            
            if (result == FtpStatus.Success)
            {
                var metadataOptions = MetadataOptions.FromBaseOptions(options);
                return await GetBlobMetadataInternalAsync(metadataOptions, cancellationToken);
            }
            
            return Result<BlobMetadata>.Fail($"Upload failed with status: {result}");
        }
        catch (OperationCanceledException ex)
        {
            _logger?.LogInformation("Upload operation was cancelled for file: {FileName}", options.FullPath);
            return Result<BlobMetadata>.Fail(ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to upload file: {FileName}", options.FullPath);
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
            
            using var stream = StorageClient.OpenRead(remotePath);
            await localFile.CopyFromStreamAsync(stream, cancellationToken);
            
            // Get file metadata
            var fileInfo = StorageClient.GetObjectInfo(remotePath);
            var relativePath = GetRelativePath(remotePath);
            
            localFile.BlobMetadata = new BlobMetadata
            {
                FullName = relativePath,
                Name = Path.GetFileName(relativePath),
                Uri = BuildUri(relativePath),
                Container = StorageOptions.RemoteDirectory,
                Length = (ulong)Math.Max(0, fileInfo.Size),
                LastModified = fileInfo.Modified,
                CreatedOn = fileInfo.Created != DateTime.MinValue ? fileInfo.Created : fileInfo.Modified,
                MimeType = GetMimeType(fileInfo.Name),
                Metadata = new Dictionary<string, string>
                {
                    [MetadataKeys.FtpRawPermissions] = fileInfo.RawPermissions ?? string.Empty,
                    [MetadataKeys.FtpFileType] = fileInfo.Type switch
                    {
                        FtpObjectType.File => MetadataValues.FileTypes.File,
                        FtpObjectType.Directory => MetadataValues.FileTypes.Directory,
                        FtpObjectType.Link => MetadataValues.FileTypes.SymbolicLink,
                        _ => MetadataValues.FileTypes.Unknown
                    }
                }
            };
            
            return Result<LocalFile>.Succeed(localFile);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download file: {FileName}", options.FullPath);
            return Result<LocalFile>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> DeleteInternalAsync(DeleteOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            
            var remotePath = BuildPath(options.FullPath);
            var exists = StorageClient.FileExists(remotePath);
            
            if (!exists)
            {
                return Result<bool>.Succeed(false);
            }
            
            StorageClient.DeleteFile(remotePath);
            return Result<bool>.Succeed(true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete file: {FileName}", options.FullPath);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<bool>> ExistsInternalAsync(ExistOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            
            var remotePath = BuildPath(options.FullPath);
            var exists = StorageClient.FileExists(remotePath);
            
            return Result<bool>.Succeed(exists);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check file existence: {FileName}", options.FullPath);
            return Result<bool>.Fail(ex);
        }
    }

    protected override async Task<Result<BlobMetadata>> GetBlobMetadataInternalAsync(MetadataOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureConnected();
            
            var remotePath = BuildPath(options.FullPath);
            var fileInfo = StorageClient.GetObjectInfo(remotePath);
            
            if (fileInfo == null)
            {
                return Result<BlobMetadata>.Fail("File not found");
            }
            
            var relativePath = GetRelativePath(remotePath);
            
            var metadata = new BlobMetadata
            {
                FullName = relativePath,
                Name = fileInfo.Name,
                Uri = BuildUri(relativePath),
                Container = StorageOptions.RemoteDirectory,
                Length = (ulong)Math.Max(0, fileInfo.Size),
                LastModified = fileInfo.Modified,
                CreatedOn = fileInfo.Created != DateTime.MinValue ? fileInfo.Created : fileInfo.Modified,
                MimeType = GetMimeType(fileInfo.Name),
                Metadata = new Dictionary<string, string>
                {
                    [MetadataKeys.FtpRawPermissions] = fileInfo.RawPermissions ?? string.Empty,
                    [MetadataKeys.FtpFileType] = fileInfo.Type switch
                    {
                        FtpObjectType.File => MetadataValues.FileTypes.File,
                        FtpObjectType.Directory => MetadataValues.FileTypes.Directory,
                        FtpObjectType.Link => MetadataValues.FileTypes.SymbolicLink,
                        _ => MetadataValues.FileTypes.Unknown
                    }
                }
            };
            
            return Result<BlobMetadata>.Succeed(metadata);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get blob metadata: {FileName}", options.FullPath);
            return Result<BlobMetadata>.Fail(ex);
        }
    }

    protected override async Task<Result> SetLegalHoldInternalAsync(bool hasLegalHold, LegalHoldOptions options,
        CancellationToken cancellationToken = default)
    {
        // FTP doesn't support legal hold, return success as no-op
        await Task.CompletedTask;
        return Result.Succeed();
    }

    protected override async Task<Result<bool>> HasLegalHoldInternalAsync(LegalHoldOptions options, CancellationToken cancellationToken = default)
    {
        // FTP doesn't support legal hold
        await Task.CompletedTask;
        return Result<bool>.Succeed(false);
    }

    private void ConfigureFtpClient(FtpClient client)
    {
        if (string.IsNullOrEmpty(StorageOptions.Host))
        {
            throw new ArgumentException("Host must be specified", nameof(StorageOptions.Host));
        }

        client.Host = StorageOptions.Host;
        client.Port = StorageOptions.Port;
        client.Config.ConnectTimeout = StorageOptions.ConnectTimeout;
        client.Config.DataConnectionType = StorageOptions.DataConnectionType;

        // Configure credentials
        if (!string.IsNullOrEmpty(StorageOptions.Username))
        {
            client.Credentials = new System.Net.NetworkCredential(StorageOptions.Username, StorageOptions.Password ?? string.Empty);
        }

        // Configure based on options type
        switch (StorageOptions)
        {
            case FtpStorageOptions ftpOptions:
                ConfigureFtpOptions(client, ftpOptions);
                break;
            case FtpsStorageOptions ftpsOptions:
                ConfigureFtpsOptions(client, ftpsOptions);
                break;
            case SftpStorageOptions sftpOptions:
                ConfigureSftpOptions(client, sftpOptions);
                break;
        }
    }

    private static void ConfigureFtpOptions(FtpClient client, FtpStorageOptions options)
    {
        client.Config.EncryptionMode = options.EncryptionMode;
        client.Config.SslProtocols = options.SslProtocols;
        client.Config.ValidateAnyCertificate = options.ValidateAnyCertificate;
    }

    private static void ConfigureFtpsOptions(FtpClient client, FtpsStorageOptions options)
    {
        client.Config.EncryptionMode = options.EncryptionMode;
        client.Config.SslProtocols = options.SslProtocols;
        client.Config.ValidateAnyCertificate = options.ValidateAnyCertificate;
        client.Config.DataConnectionEncryption = options.DataConnectionEncryption;
        
        if (!string.IsNullOrEmpty(options.ClientCertificatePath))
        {
            var certificate = !string.IsNullOrEmpty(options.ClientCertificatePassword)
                ? new X509Certificate2(options.ClientCertificatePath, options.ClientCertificatePassword)
                : new X509Certificate2(options.ClientCertificatePath);
            
            client.Config.ClientCertificates.Add(certificate);
        }
    }

    private static void ConfigureSftpOptions(FtpClient client, SftpStorageOptions options)
    {
        // Note: FluentFTP doesn't directly support SFTP, this is a placeholder for the pattern
        // In a real implementation, you might use SSH.NET or similar for SFTP
        throw new NotSupportedException("SFTP support requires SSH.NET library integration. Please use FTP or FTPS options.");
    }

    private void EnsureConnected()
    {
        try
        {
            if (!StorageClient.IsConnected)
            {
                _logger?.LogDebug("Connecting to FTP server at {Host}:{Port}", StorageOptions.Host, StorageOptions.Port);
                StorageClient.Connect();
            }
            
            // Test the connection by getting the working directory
            var workingDir = StorageClient.GetWorkingDirectory();
            _logger?.LogDebug("Successfully connected to FTP server. Working directory: {WorkingDir}", workingDir);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to FTP server at {Host}:{Port}", StorageOptions.Host, StorageOptions.Port);
            throw;
        }
    }

    private string BuildPath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return StorageOptions.RemoteDirectory ?? "/";
        }

        var basePath = StorageOptions.RemoteDirectory ?? "/";
        
        // Normalize path separators
        basePath = basePath.Replace('\\', '/');
        fileName = fileName.Replace('\\', '/');
        
        // Remove leading slash from filename if base path doesn't end with slash
        if (basePath.EndsWith("/"))
        {
            fileName = fileName.TrimStart('/');
        }
        else if (fileName.StartsWith("/"))
        {
            return fileName;
        }
        
        return $"{basePath.TrimEnd('/')}/{fileName.TrimStart('/')}";
    }

    private string GetRelativePath(string fullPath)
    {
        if (string.IsNullOrEmpty(StorageOptions.RemoteDirectory) || StorageOptions.RemoteDirectory == "/")
        {
            return fullPath.TrimStart('/');
        }

        var basePath = StorageOptions.RemoteDirectory.TrimEnd('/');
        if (fullPath.StartsWith(basePath + "/"))
        {
            return fullPath.Substring(basePath.Length + 1);
        }

        return fullPath.TrimStart('/');
    }

    private Uri? BuildUri(string fileName)
    {
        if (string.IsNullOrEmpty(StorageOptions.Host))
        {
            return null;
        }

        var scheme = StorageOptions switch
        {
            FtpsStorageOptions => "ftps",
            SftpStorageOptions => "sftp",
            _ => "ftp"
        };

        var port = StorageOptions.Port;
        var defaultPort = scheme switch
        {
            "ftp" => 21,
            "ftps" => 990,
            "sftp" => 22,
            _ => 21
        };

        var uriBuilder = new UriBuilder(scheme, StorageOptions.Host)
        {
            Path = BuildPath(fileName)
        };

        if (port != defaultPort)
        {
            uriBuilder.Port = port;
        }

        return uriBuilder.Uri;
    }

    private static string? GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    public new void Dispose()
    {
        if (StorageClient != null)
        {
            if (StorageClient.IsConnected)
            {
                try
                {
                    StorageClient.Disconnect();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error disconnecting FTP client");
                }
            }
            StorageClient?.Dispose();
        }
        
        base.Dispose();
    }
}

/// <summary>
/// Wrapper for FTP write stream to ensure proper disposal and persistence.
/// </summary>
internal class FtpWriteStreamWrapper : Stream
{
    private readonly Stream _innerStream;
    private readonly FtpClient _ftpClient;
    private readonly string _remotePath;
    private readonly ILogger? _logger;
    private bool _disposed = false;

    public FtpWriteStreamWrapper(Stream innerStream, FtpClient ftpClient, string remotePath, ILogger? logger)
    {
        _innerStream = innerStream;
        _ftpClient = ftpClient;
        _remotePath = remotePath;
        _logger = logger;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position 
    { 
        get => _innerStream.Position; 
        set => _innerStream.Position = value; 
    }

    public override void Flush() => _innerStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);
    public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => _innerStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => 
        _innerStream.WriteAsync(buffer, offset, count, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _innerStream?.Flush();
                _innerStream?.Dispose();
                
                // Verify the file was created
                if (_ftpClient.IsConnected && !_ftpClient.FileExists(_remotePath))
                {
                    _logger?.LogWarning("File {RemotePath} was not persisted after stream disposal", _remotePath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing FTP write stream for {RemotePath}", _remotePath);
            }
            finally
            {
                _disposed = true;
            }
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                await _innerStream.FlushAsync();
                await _innerStream.DisposeAsync();
                
                // Verify the file was created
                if (_ftpClient.IsConnected && !_ftpClient.FileExists(_remotePath))
                {
                    _logger?.LogWarning("File {RemotePath} was not persisted after stream disposal", _remotePath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing FTP write stream for {RemotePath}", _remotePath);
            }
            finally
            {
                _disposed = true;
            }
        }
        await base.DisposeAsync();
    }
}

/// <summary>
/// Memory-based write stream that uploads to FTP on disposal.
/// </summary>
internal class FtpMemoryWriteStream : MemoryStream
{
    private readonly FtpClient _ftpClient;
    private readonly string _remotePath;
    private readonly ILogger? _logger;
    private bool _disposed = false;

    public FtpMemoryWriteStream(FtpClient ftpClient, string remotePath, ILogger? logger)
    {
        _ftpClient = ftpClient;
        _remotePath = remotePath;
        _logger = logger;
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                // Upload the memory stream content
                Position = 0;
                var result = _ftpClient.UploadStream(this, _remotePath, FtpRemoteExists.Overwrite, false);
                if (result != FtpStatus.Success)
                {
                    _logger?.LogError("Failed to upload stream content to {RemotePath}, status: {Status}", _remotePath, result);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error uploading memory stream to {RemotePath}", _remotePath);
            }
            finally
            {
                _disposed = true;
            }
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                // Upload the memory stream content
                Position = 0;
                var result = _ftpClient.UploadStream(this, _remotePath, FtpRemoteExists.Overwrite, false);
                if (result != FtpStatus.Success)
                {
                    _logger?.LogError("Failed to upload stream content to {RemotePath}, status: {Status}", _remotePath, result);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error uploading memory stream to {RemotePath}", _remotePath);
            }
            finally
            {
                _disposed = true;
            }
        }
        await base.DisposeAsync();
    }
}