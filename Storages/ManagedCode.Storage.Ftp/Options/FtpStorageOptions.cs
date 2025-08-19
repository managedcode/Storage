using System.Security.Authentication;
using System.Text;
using FluentFTP;

namespace ManagedCode.Storage.Ftp.Options;

/// <summary>
/// FTP storage options for standard FTP connections.
/// </summary>
public class FtpStorageOptions : IFtpStorageOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 21;
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
    /// Gets or sets the FTP connection security protocol.
    /// </summary>
    public FtpEncryptionMode EncryptionMode { get; set; } = FtpEncryptionMode.None;
    
    /// <summary>
    /// Gets or sets SSL/TLS protocols to use for FTPS.
    /// </summary>
    public SslProtocols SslProtocols { get; set; } = SslProtocols.None;
    
    /// <summary>
    /// Gets or sets whether to validate SSL certificates.
    /// </summary>
    public bool ValidateAnyCertificate { get; set; } = true;
}