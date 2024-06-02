using System;

namespace ManagedCode.Storage.Client;

public record ProgressStatus(
    string File,
    float Progress,
    long TotalBytes,
    long TransferredBytes,
    TimeSpan Elapsed,
    TimeSpan Remaining,
    string Speed,
    string? Error = null);