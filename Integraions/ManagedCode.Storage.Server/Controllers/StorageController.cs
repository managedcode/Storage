using ManagedCode.Storage.Core;
using ManagedCode.Storage.Server.ChunkUpload;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.Server.Controllers;

/// <summary>
/// Default storage controller exposing all storage endpoints using the shared <see cref="IStorage"/> instance.
/// </summary>
[Route("api/storage")]
public class StorageController : StorageControllerBase<IStorage>
{
    /// <summary>
    /// Initialises a new instance of the default storage controller.
    /// </summary>
    /// <param name="storage">The shared storage instance.</param>
    /// <param name="chunkUploadService">Chunk upload coordinator.</param>
    /// <param name="options">Server behaviour options.</param>
    public StorageController(
        IStorage storage,
        ChunkUploadService chunkUploadService,
        StorageServerOptions options) : base(storage, chunkUploadService, options)
    {
    }
}
