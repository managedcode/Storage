using System;
using ManagedCode.Storage.Server.Models;

namespace ManagedCode.Storage.Server.ChunkUpload;

internal static class ChunkUploadDescriptor
{
    public static string ResolveUploadId(FilePayload payload)
    {
        return string.IsNullOrWhiteSpace(payload.UploadId)
            ? throw new InvalidOperationException("UploadId must be provided for chunk uploads.")
            : payload.UploadId;
    }
}
