using ManagedCode.Storage.SampleClient.Core.Enums;
using ManagedCode.Storage.SampleClient.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace ManagedCode.Storage.SampleClient.WebApi;

[Route("api/[controller]")]
public class FileSystemStorageController : BaseStorageController
{
    public FileSystemStorageController(CurrentState currentState)
    {
        currentState.StorageProvider = StorageProvider.FileSystem;
    }
}
