using FluentFTP;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Ftp.Options;

/// <summary>
/// Represents the base interface for FTP storage options.
/// </summary>
public interface IFtpStorageOptions : IStorageOptions
{
    /// <summary>
    /// Gets or sets the FTP server hostname or IP address.
    /// </summary>
    string? Host { get; set; }
    
    /// <summary>
    /// Gets or sets the FTP server port. Default is 21 for FTP, 22 for SFTP, 990 for FTPS.
    /// </summary>
    int Port { get; set; }
    
    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    string? Username { get; set; }
    
    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    string? Password { get; set; }
    
    /// <summary>
    /// Gets or sets the remote directory path to use as the container/root.
    /// </summary>
    string? RemoteDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the connection timeout in milliseconds.
    /// </summary>
    int ConnectTimeout { get; set; }
    
    /// <summary>
    /// Gets or sets the read/write timeout in milliseconds.
    /// </summary>
    int DataConnectionTimeout { get; set; }
    
    /// <summary>
    /// Gets or sets whether to create the remote directory if it doesn't exist.
    /// </summary>
    bool CreateDirectoryIfNotExists { get; set; }
    
    /// <summary>
    /// Gets or sets the data connection type (AutoPassive, PASV, EPSV, PORT, EPRT).
    /// </summary>
    FtpDataConnectionType DataConnectionType { get; set; }
    
    /// <summary>
    /// Gets or sets the FTP encoding.
    /// </summary>
    System.Text.Encoding? Encoding { get; set; }
}