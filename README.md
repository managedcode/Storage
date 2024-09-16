<h1>Blob Storage API Client</h1>
This project provides a universal wrapper for working with different cloud blob storage providers (e.g., Azure Blob Storage, AWS S3, Google Cloud Storage). The solution supports operations like uploading, downloading, and deleting files, including handling large files by uploading them in chunks.

Key Features
Universal Interface: Work seamlessly with multiple blob storage providers using a unified API.
File Operations: Upload, download, and delete files of various sizes (from small to large files up to 1GB).
Chunked Uploads: Support for uploading large files in chunks for better performance and reliability.
Metadata Retrieval: Access metadata information for stored files.
Prerequisites
.NET 6.0+
A supported blob storage service (Azure, AWS S3, Google Cloud Storage, etc.).
A valid API token for authentication.
Getting Started
Installation
To use the library in your project, add the following NuGet package:

dotnet add package BlobStorageClient
Configuration
You need to configure the IStorage interface in your service's Startup.cs or Program.cs to connect to your cloud blob storage provider.

csharp
Copy code
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<IStorage, AzureBlobStorage>(); // Example for Azure Blob Storage
}
Dependency Injection Example
The API client is injected as a service. Here is an example of using it within your service class.

csharp
Copy code
public class FileService
{
    private readonly IStorage _storage;

    public FileService(IStorage storage)
    {
        _storage = storage;
    }

    public async Task UploadFileAsync(Stream fileStream, string fileName)
    {
        await _storage.UploadAsync(fileStream, fileName);
    }
}
Public API Endpoints
This API provides several endpoints for file operations. Below is an outline of the available operations:

Upload File
Endpoint: /file/upload
Method: POST
Description: Uploads a file to the server. For large files, it automatically uploads in chunks.
Request: Multipart form-data containing the file to upload.
Response: 200 OK on success or 400 Bad Request on error.
Download File
Endpoint: /file/download/{fileName}
Method: GET
Description: Downloads a file by its name.
Response: Returns the file as a binary stream or 404 Not Found if the file is missing.
Delete File
Endpoint: /file/delete/{fileName}
Method: DELETE
Description: Deletes a file from the storage by name.
Response: 200 OK on success or 404 Not Found.
Get File Metadata
Endpoint: /file/metadata/{fileName}
Method: GET
Description: Retrieves metadata for the file such as its size and creation date.
Response: JSON object containing file metadata or 404 Not Found.
Example Usage
Uploading a File
You can upload a file using a simple HTTP request. Here’s an example using curl:



Development Setup
To set up the development environment for this project:

Clone the repository:

bash
Copy code
git clone https://github.com/your-username/BlobStorageClient.git
cd BlobStorageClient
Build the project:

bash
Copy code
dotnet build
Run the API locally:

bash
Copy code
dotnet run
Contributing
We welcome contributions to improve the project. To contribute:

Fork the repository.
Create a feature branch.
Commit your changes.
Open a Pull Request with a detailed description.