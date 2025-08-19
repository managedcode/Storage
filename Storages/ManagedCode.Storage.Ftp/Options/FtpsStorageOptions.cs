using System.Security.Authentication;
using System.Text;
using FluentFTP;

namespace ManagedCode.Storage.Ftp.Options;

/// <summary>
/// FTPS storage options for FTP over SSL/TLS connections.
/// </summary>
public class FtpsStorageOptions : IFtpStorageOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 990; // Implicit FTPS port, 21 for explicit
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
    /// Gets or sets the FTP encryption mode (Explicit or Implicit).
    /// </summary>
    public FtpEncryptionMode EncryptionMode { get; set; } = FtpEncryptionMode.Implicit;
    
    /// <summary>
    /// Gets or sets SSL/TLS protocols to use.
    /// </summary>
    public SslProtocols SslProtocols { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;
    
    /// <summary>
    /// Gets or sets whether to validate SSL certificates.
    /// </summary>
    public bool ValidateAnyCertificate { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the client certificate path for mutual authentication.
    /// </summary>
    public string? ClientCertificatePath { get; set; }
    
    /// <summary>
    /// Gets or sets the client certificate password.
    /// </summary>
    public string? ClientCertificatePassword { get; set; }
    
    /// <summary>
    /// Gets or sets whether to use data connection encryption.
    /// </summary>
    public bool DataConnectionEncryption { get; set; } = true;
}