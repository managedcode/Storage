using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Sftp.Options;

/// <summary>
/// Strongly-typed configuration for the SSH-based SFTP storage provider.
/// </summary>
public class SftpStorageOptions : IStorageOptions
{
    /// <summary>
    /// SFTP host name or IP address.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// SFTP port, defaults to 22.
    /// </summary>
    public int Port { get; set; } = 22;

    /// <summary>
    /// Username used for authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password used for authentication. Optional when key-based auth is configured.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Remote directory that acts as the container root.
    /// </summary>
    public string? RemoteDirectory { get; set; } = "/";

    /// <summary>
    /// Connection timeout in milliseconds.
    /// </summary>
    public int ConnectTimeout { get; set; } = 15000;

    /// <summary>
    /// Logical timeout for long running data operations in milliseconds.
    /// </summary>
    public int OperationTimeout { get; set; } = 15000;

    /// <summary>
    /// Automatically create directories when uploading files.
    /// </summary>
    public bool CreateDirectoryIfNotExists { get; set; } = true;

    /// <summary>
    /// Automatically create the container root when connecting.
    /// </summary>
    public bool CreateContainerIfNotExists { get; set; } = true;

    /// <summary>
    /// Path to an SSH private key file for key-based authentication.
    /// </summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>
    /// Passphrase protecting the SSH private key.
    /// </summary>
    public string? PrivateKeyPassphrase { get; set; }

    /// <summary>
    /// Inline SSH private key content used instead of <see cref="PrivateKeyPath"/>.
    /// </summary>
    public string? PrivateKeyContent { get; set; }

    /// <summary>
    /// Accept any host key presented by the server (not recommended for production).
    /// </summary>
    public bool AcceptAnyHostKey { get; set; } = true;

    /// <summary>
    /// Expected host key fingerprint when <see cref="AcceptAnyHostKey"/> is <c>false</c>.
    /// </summary>
    public string? HostKeyFingerprint { get; set; }
}
