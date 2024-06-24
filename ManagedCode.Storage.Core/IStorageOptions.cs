namespace ManagedCode.Storage.Core;

/// <summary>
/// Provides the options for storage operations.
/// </summary>
public interface IStorageOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to create the container if it does not exist.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the container should be created if it does not exist; otherwise, <c>false</c>.
    /// </value>
    public bool CreateContainerIfNotExists { get; set; }
}