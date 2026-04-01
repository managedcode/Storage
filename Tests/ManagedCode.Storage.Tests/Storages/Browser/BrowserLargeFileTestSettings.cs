namespace ManagedCode.Storage.Tests.Storages.Browser;

internal static class BrowserLargeFileTestSettings
{
    public const int DefaultLargePayloadSizeMiB = 128;
    public const int StressPayloadSizeMiB = 256;
    public const long BytesPerMiB = 1024L * 1024L;
    public const float DefaultLargeTimeoutMs = 180000;
    public const float StressTimeoutMs = 180000;
}
