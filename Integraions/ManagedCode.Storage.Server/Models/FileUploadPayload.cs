using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Server.Models;

public class FileUploadPayload
{
    public IFormFile File { get; set; }
    public FilePayload Payload { get; set; }
}