using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cartograph.Catalog;
using Cartograph.Format;
using ManagedCode.Storage.Cartograph;
using ManagedCode.Storage.Cartograph.Options;

namespace ManagedCode.Storage.Tests.Storages.Cartograph;

internal sealed class CartographArtifactFixture : IDisposable
{
    public const int PieceSize = 64 * 1024;
    public static readonly DateTime CreatedUtc = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime ModifiedUtc = CreatedUtc.AddHours(1);
    private readonly string _root = Path.Combine(Path.GetTempPath(), "storage-cartograph", Guid.NewGuid().ToString("N"));

    public CartographArtifactFixture()
    {
        Directory.CreateDirectory(_root);
        var large = new byte[2 * 1024 * 1024 + 17];
        new Random(42).NextBytes(large);
        Files = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["root.txt"] = "Cartograph through Storage\n"u8.ToArray(),
            ["reports/large.bin"] = large,
            ["reports/empty.txt"] = [],
            ["reports/nested/дані.txt"] = "Привіт із Cartograph!"u8.ToArray(),
            ["reports-old/other.txt"] = "different directory"u8.ToArray()
        };
        ArtifactPath = WriteArtifact("files.ctg");
    }

    public string ArtifactPath { get; }
    public IReadOnlyDictionary<string, byte[]> Files { get; }
    public string DestinationPath => Path.Combine(_root, "download.bin");
    public CartographStorage CreateStorage(string? path = null) => new(new CartographStorageOptions { ArtifactPath = path ?? ArtifactPath });

    public string WriteArtifact(string name, Action<List<CatalogEntry>>? editCatalog = null)
    {
        var writer = new SegmentedArtifactWriter();
        var catalogSegment = writer.AddSegment();
        var content = writer.AddSegment();
        var entries = Files.Select(file => AddFile(content, file.Key, file.Value)).ToList();
        editCatalog?.Invoke(entries);
        var catalog = new FileCatalog
        {
            SourceRoot = "/a/source/root/that/need/not/exist",
            CreatedUtc = CreatedUtc,
            GroupingMode = "flat",
            GroupNames = ["files"],
            Entries = entries
        };
        catalogSegment.AddRecord(catalog.Serialize());
        var path = Path.Combine(_root, name);
        writer.Save(path);
        return path;
    }

    private CatalogEntry AddFile(SegmentBuilder content, string relativePath, byte[] bytes)
    {
        var source = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".source");
        File.WriteAllBytes(source, bytes);
        var start = content.RecordCount;
        if (bytes.Length == 0)
            content.AddRecord([]);
        for (var offset = 0; offset < bytes.Length; offset += PieceSize)
            content.AddFileRecord(source, offset, Math.Min(PieceSize, bytes.Length - offset));

        return new CatalogEntry
        {
            RelativePath = relativePath,
            Length = bytes.Length,
            LastWriteUtcTicks = ModifiedUtc.Ticks,
            Checksum = 0,
            SegmentIndex = 1,
            RecordIndex = start,
            GlobalIndex = start + 1,
            RecordCount = content.RecordCount - start
        };
    }

    public void CorruptFirstPayload()
    {
        long offset;
        using (var artifact = Artifact.Open(ArtifactPath))
            offset = artifact.Segments[1].PayloadOffset;

        using var file = File.Open(ArtifactPath, FileMode.Open, FileAccess.ReadWrite);
        file.Position = offset;
        var original = file.ReadByte();
        file.Position = offset;
        file.WriteByte((byte)(original ^ 0xff));
    }

    public static CatalogEntry Change(CatalogEntry entry, string? path = null, long? length = null,
        int? segment = null, int? record = null, long? global = null, int? count = null, long? ticks = null) => new()
        {
            RelativePath = path ?? entry.RelativePath,
            Length = length ?? entry.Length,
            SegmentIndex = segment ?? entry.SegmentIndex,
            RecordIndex = record ?? entry.RecordIndex,
            GlobalIndex = global ?? entry.GlobalIndex,
            RecordCount = count ?? entry.RecordCount,
            LastWriteUtcTicks = ticks ?? entry.LastWriteUtcTicks,
            Checksum = entry.Checksum
        };

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
