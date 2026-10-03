using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.Cartograph.Options;

/// <summary>Identifies an existing immutable Cartograph artifact containing a file catalog.</summary>
public sealed class CartographStorageOptions : IStorageOptions
{
    public string ArtifactPath { get; set; } = string.Empty;

    /// <summary>Artifacts must be created with Cartograph's writer; this provider never creates them.</summary>
    public bool CreateContainerIfNotExists { get; set; }
}
