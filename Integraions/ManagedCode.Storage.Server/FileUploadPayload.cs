using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Server;

public class FileUploadPayload
{
    public IFormFile File { get; set; }
    public FilePayload Payload { get; set; }
}