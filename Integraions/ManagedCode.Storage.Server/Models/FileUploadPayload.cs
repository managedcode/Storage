using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Server.Models;

public class FileUploadPayload
{
    public IFormFile File { get; set; } = default!;
    public FilePayload Payload { get; set; } = new();
}
