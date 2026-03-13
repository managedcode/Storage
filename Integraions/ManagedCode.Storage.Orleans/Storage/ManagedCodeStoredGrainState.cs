namespace Orleans.Storage;

public sealed class ManagedCodeStoredGrainState<T>
{
    public T? State { get; set; }

    public string? ETag { get; set; }

    public bool RecordExists { get; set; } = true;
}
