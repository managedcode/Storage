using System;
using Azure.Core;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Azure;

/// <summary>Creates container-scoped storage without exposing native storage clients.</summary>
public static class AzureStorageConnection
{
    public static IStorage Create(string connection, string containerName, TokenCredential? credential = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        if (!Uri.TryCreate(connection, UriKind.Absolute, out var endpoint))
        {
            return new AzureStorage(new AzureStorageOptions
            {
                ConnectionString = connection,
                Container = containerName,
                CreateContainerIfNotExists = false
            });
        }

        ArgumentNullException.ThrowIfNull(credential);
        if (endpoint.Scheme != Uri.UriSchemeHttps || endpoint.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new ArgumentException("Storage service endpoint must be an HTTPS origin.", nameof(connection));

        return new AzureStorage(new AzureStorageCredentialsOptions
        {
            ServiceUri = endpoint,
            ContainerName = containerName,
            Container = containerName,
            Credentials = credential,
            CreateContainerIfNotExists = false
        });
    }
}
