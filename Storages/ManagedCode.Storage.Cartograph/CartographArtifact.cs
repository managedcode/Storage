using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cartograph.Catalog;
using Cartograph.Format;

namespace ManagedCode.Storage.Cartograph;

internal sealed class CartographArtifact : IDisposable
{
    private readonly Dictionary<string, CatalogEntry> _entries = new(StringComparer.Ordinal);

    private CartographArtifact(Artifact artifact, string path)
    {
        Artifact = artifact;
        Path = path;
        if (artifact.Segments.Count == 0 || artifact.Segments[0].RecordCount != 1)
            throw new InvalidDataException("The first segment must contain a single file catalog record.");

        using var record = artifact.ReadRecord(0);
        Catalog = FileCatalog.Deserialize(record.Sequence);
        foreach (var entry in Catalog.Entries)
        {
            ValidateEntry(entry);
            if (!_entries.TryAdd(entry.RelativePath, entry))
                throw new InvalidDataException($"Duplicate catalog path '{entry.RelativePath}'.");
        }
    }

    public Artifact Artifact { get; }
    public FileCatalog Catalog { get; }
    public string Path { get; }

    public static CartographArtifact Open(string path)
    {
        var artifact = Artifact.Open(path);
        try
        {
            return new CartographArtifact(artifact, path);
        }
        catch
        {
            artifact.Dispose();
            throw;
        }
    }

    public CatalogEntry? Find(string path) => _entries.GetValueOrDefault(CartographPath.Normalize(path));

    public CatalogEntry RequireEntry(string path) => Find(path)
        ?? throw new FileNotFoundException($"File '{path}' is not present in the artifact.", path);

    public Stream OpenRead(string path)
    {
        var entry = RequireEntry(path);
        return new CartographReadStream(this, entry);
    }

    private void ValidateEntry(CatalogEntry entry)
    {
        CartographPath.ValidateCatalogPath(entry.RelativePath);
        if (entry.Length < 0 || entry.SegmentIndex <= 0 || entry.SegmentIndex >= Artifact.Segments.Count ||
            entry.RecordIndex < 0 || entry.RecordCount <= 0 ||
            entry.LastWriteUtcTicks < DateTime.MinValue.Ticks || entry.LastWriteUtcTicks > DateTime.MaxValue.Ticks)
        {
            throw new InvalidDataException($"Invalid catalog entry '{entry.RelativePath}'.");
        }

        var segment = Artifact.Segments[entry.SegmentIndex];
        if (entry.RecordIndex > segment.RecordCount || entry.RecordCount > segment.RecordCount - entry.RecordIndex)
            throw new InvalidDataException($"Catalog record span is out of bounds for '{entry.RelativePath}'.");

        var globalStart = Artifact.Segments.Take(entry.SegmentIndex).Sum(item => (long)item.RecordCount);
        if (entry.GlobalIndex != globalStart + entry.RecordIndex)
            throw new InvalidDataException($"Catalog record indices disagree for '{entry.RelativePath}'.");

        long length = 0;
        for (var index = 0; index < entry.RecordCount; index++)
            length = checked(length + segment.GetRecordLength(entry.RecordIndex + index));

        if (length != entry.Length)
            throw new InvalidDataException($"Catalog length disagrees with records for '{entry.RelativePath}'.");
    }

    public void Dispose() => Artifact.Dispose();
}
