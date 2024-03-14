using ManagedCode.Storage.SampleClient.Core.Enums;

namespace ManagedCode.Storage.SampleClient.WebApi.Models;

// Scoped object for internal session-depending logic
public class CurrentState
{
    // Affects what storage will be injected to IStorage dependency
    public StorageProvider? StorageProvider { get; set; }
}
