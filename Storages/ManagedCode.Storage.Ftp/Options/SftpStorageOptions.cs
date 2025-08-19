using System.Text;
using FluentFTP;

namespace ManagedCode.Storage.Ftp.Options;

/// <summary>
/// SFTP storage options for secure FTP over SSH connections.
/// </summary>
public class SftpStorageOptions : IFtpStorageOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 22;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? RemoteDirectory { get; set; } = "/";
    public int ConnectTimeout { get; set; } = 15000;
    public int DataConnectionTimeout { get; set; } = 15000;
    public bool CreateDirectoryIfNotExists { get; set; } = true;
    public bool CreateContainerIfNotExists { get; set; } = true;
    public FtpDataConnectionType DataConnectionType { get; set; } = FtpDataConnectionType.AutoPassive;
    public Encoding? Encoding { get; set; } = Encoding.UTF8;
    
    /// <summary>
    /// Gets or sets the path to the SSH private key file for authentication.
    /// </summary>
    public string? PrivateKeyPath { get; set; }
    
    /// <summary>
    /// Gets or sets the passphrase for the SSH private key.
    /// </summary>
    public string? PrivateKeyPassphrase { get; set; }
    
    /// <summary>
    /// Gets or sets the SSH private key content as a string.
    /// </summary>
    public string? PrivateKeyContent { get; set; }
    
    /// <summary>
    /// Gets or sets whether to accept any SSH host key (not recommended for production).
    /// </summary>
    public bool AcceptAnyHostKey { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the expected SSH host key fingerprint for validation.
    /// </summary>
    public string? HostKeyFingerprint { get; set; }
}