using System.Collections.Generic;

namespace ManagedCode.Storage.Core.Models;

public class UploadOptions
{
    public UploadOptions()
    {
    }

    public UploadOptions(string? fileName = null, string? directory = null, string? mimeType = null, Dictionary<string, string>? metadata = null,
        string? fileNamePrefix = null)
    {
        FileName = fileName;
        MimeType = mimeType;
        Directory = directory;
        Metadata = metadata;
        FileNamePrefix = fileNamePrefix;
    }

    public string? FileName { get; set; }
    public string? FileNamePrefix { get; set; }
    public string? Directory { get; set; }
    public string? MimeType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}