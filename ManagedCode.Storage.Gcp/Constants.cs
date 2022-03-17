namespace ManagedCode.Storage.Gcp;

public static class Constants
{
    private const string ContentType = "application/octet-stream";
    private const string NameRegex = @"(?<Container>[^/]+)/(?<Blob>.+)";
}