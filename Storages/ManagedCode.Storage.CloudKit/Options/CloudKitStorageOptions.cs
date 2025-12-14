using System.Net.Http;
using ManagedCode.Storage.CloudKit.Clients;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.CloudKit.Options;

public class CloudKitStorageOptions : IStorageOptions
{
    public bool CreateContainerIfNotExists { get; set; }

    /// <summary>
    /// CloudKit container identifier, e.g. <c>iCloud.com.company.app</c>.
    /// </summary>
    public string ContainerId { get; set; } = string.Empty;

    public CloudKitEnvironment Environment { get; set; } = CloudKitEnvironment.Development;

    public CloudKitDatabase Database { get; set; } = CloudKitDatabase.Public;

    /// <summary>
    /// Optional prefix applied to all blob paths (like a virtual folder).
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// CloudKit record type that stores files.
    /// </summary>
    public string RecordType { get; set; } = "MCStorageFile";

    public string PathFieldName { get; set; } = "path";

    public string AssetFieldName { get; set; } = "file";

    public string ContentTypeFieldName { get; set; } = "contentType";

    /// <summary>
    /// API token authentication (<c>ckAPIToken</c>) for CloudKit Web Services.
    /// </summary>
    public string? ApiToken { get; set; }

    /// <summary>
    /// Optional user authentication token (<c>ckWebAuthToken</c>) for private database access.
    /// </summary>
    public string? WebAuthToken { get; set; }

    /// <summary>
    /// Server-to-server key id for signed requests (<c>X-Apple-CloudKit-Request-KeyID</c>).
    /// </summary>
    public string? ServerToServerKeyId { get; set; }

    /// <summary>
    /// Server-to-server private key in PEM (PKCS8) format.
    /// </summary>
    public string? ServerToServerPrivateKeyPem { get; set; }

    /// <summary>
    /// Optional custom HttpClient used for CloudKit Web Services requests.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>
    /// Optional custom CloudKit client (useful for tests).
    /// </summary>
    public ICloudKitClient? Client { get; set; }
}

