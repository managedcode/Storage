namespace ManagedCode.Storage.Browser.Models;

internal sealed class BrowserPayloadWriteResult
{
    public required long Length { get; init; }

    public required string PayloadStore { get; init; }
}
