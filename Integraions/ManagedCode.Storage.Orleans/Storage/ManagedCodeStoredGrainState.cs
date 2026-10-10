namespace Orleans.Storage;

public sealed class ManagedCodeStoredGrainState<T>
{
    public T? State { get; set; }

    public string? ETag { get; set; }

    [System.ComponentModel.DefaultValue(true)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]
    public bool RecordExists { get; set; } = true;
}
