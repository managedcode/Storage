using Dropbox.Api;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Dropbox.Clients;

namespace ManagedCode.Storage.Dropbox.Options;

public class DropboxStorageOptions : IStorageOptions
{
    public IDropboxClientWrapper? Client { get; set; }

    public DropboxClient? DropboxClient { get; set; }

    /// <summary>
    /// OAuth2 access token (short-lived or long-lived) used to create a <see cref="global::Dropbox.Api.DropboxClient"/> when <see cref="DropboxClient"/> is not provided.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// OAuth2 refresh token used to create a <see cref="global::Dropbox.Api.DropboxClient"/> when <see cref="DropboxClient"/> is not provided.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Dropbox app key (required when using <see cref="RefreshToken"/>).
    /// </summary>
    public string? AppKey { get; set; }

    /// <summary>
    /// Dropbox app secret (optional when using PKCE refresh tokens).
    /// </summary>
    public string? AppSecret { get; set; }

    public DropboxClientConfig? DropboxClientConfig { get; set; }

    public string RootPath { get; set; } = string.Empty;

    public bool CreateContainerIfNotExists { get; set; } = true;
}
