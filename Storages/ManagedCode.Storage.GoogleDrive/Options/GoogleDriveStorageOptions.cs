using Google.Apis.Auth.OAuth2;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.GoogleDrive.Options;

/// <summary>
/// Options for configuring Google Drive storage.
/// </summary>
public class GoogleDriveStorageOptions : IStorageOptions
{
    /// <summary>
    /// Gets or sets the folder ID where files will be stored.
    /// This is required and must be a valid Google Drive folder ID.
    /// </summary>
    public string? FolderId { get; set; }

    /// <summary>
    /// Gets or sets the Service Account credential for authentication.
    /// </summary>
    public ServiceAccountCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the path to the Service Account JSON key file.
    /// Alternative to providing Credential directly.
    /// </summary>
    public string? ServiceAccountJsonPath { get; set; }

    /// <summary>
    /// Gets or sets the Service Account JSON content directly.
    /// Alternative to providing ServiceAccountJsonPath.
    /// </summary>
    public string? ServiceAccountJson { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to create the container folder if it does not exist.
    /// </summary>
    public bool CreateContainerIfNotExists { get; set; } = true;

    /// <summary>
    /// Gets or sets the application name for the Google Drive API client.
    /// </summary>
    public string ApplicationName { get; set; } = "ManagedCode.Storage.GoogleDrive";
}
