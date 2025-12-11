using ManagedCode.Storage.GoogleDrive;
using ManagedCode.Storage.GoogleDrive.Extensions;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("===========================================");
Console.WriteLine("  Google Drive Storage Sample Application  ");
Console.WriteLine("===========================================");
Console.WriteLine();

// Configuration - Update these values with your Google Drive settings
// Option 1: Use JSON content directly (preferred for cloud deployments)
var serviceAccountJson = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON");
// Option 2: Use file path (convenient for local development)
var serviceAccountJsonPath = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON_PATH");
// Required: Folder ID where files will be stored
var folderId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_FOLDER_ID");

var hasJsonContent = !string.IsNullOrEmpty(serviceAccountJson);
var hasJsonPath = !string.IsNullOrEmpty(serviceAccountJsonPath);
var hasFolderId = !string.IsNullOrEmpty(folderId);

if (!hasJsonContent && !hasJsonPath || !hasFolderId)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("WARNING: Missing required configuration.");
    Console.WriteLine();
    Console.WriteLine("To use this sample, you need to:");
    Console.WriteLine("1. Create a Google Cloud Project");
    Console.WriteLine("2. Enable the Google Drive API");
    Console.WriteLine("3. Create a Service Account and download the JSON key file");
    Console.WriteLine("4. Share a folder with the service account email");
    Console.WriteLine();
    Console.WriteLine("Required environment variables:");
    Console.WriteLine();
    Console.WriteLine("  GOOGLE_DRIVE_FOLDER_ID = folder ID where files will be stored (REQUIRED)");
    Console.WriteLine();
    Console.WriteLine("  AND one of:");
    Console.WriteLine("  GOOGLE_SERVICE_ACCOUNT_JSON = {\"type\":\"service_account\",...}");
    Console.WriteLine("  GOOGLE_SERVICE_ACCOUNT_JSON_PATH = C:\\path\\to\\key.json");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Running in demo mode with mock operations...");
    Console.WriteLine();
    await RunDemoModeAsync();
    return;
}

Console.WriteLine($"Using credentials from: {(hasJsonContent ? "GOOGLE_SERVICE_ACCOUNT_JSON (JSON content)" : "GOOGLE_SERVICE_ACCOUNT_JSON_PATH (file path)")}");
Console.WriteLine();

// Configure services
var services = new ServiceCollection();

services.AddGoogleDriveStorageAsDefault(options =>
{
    // Prefer JSON content over file path
    if (hasJsonContent)
    {
        options.ServiceAccountJson = serviceAccountJson;
    }
    else
    {
        options.ServiceAccountJsonPath = serviceAccountJsonPath;
    }

    options.FolderId = folderId;
    options.ApplicationName = "GoogleDriveSample";
});

var serviceProvider = services.BuildServiceProvider();
var storage = serviceProvider.GetRequiredService<IGoogleDriveStorage>();

try
{
    await RunStorageOperationsAsync(storage);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: {ex.Message}");
    Console.ResetColor();
}

async Task RunStorageOperationsAsync(IGoogleDriveStorage storage)
{
    Console.WriteLine("1. Creating container (folder)...");
    var containerResult = await storage.CreateContainerAsync();
    Console.WriteLine($"   Container created: {containerResult.IsSuccess}");
    Console.WriteLine();

    // Upload a text file
    Console.WriteLine("2. Uploading a text file...");
    var textContent = "Hello from ManagedCode.Storage.GoogleDrive!\nThis is a sample text file.";
    var uploadResult = await storage.UploadAsync(textContent, options =>
    {
        options.FileName = "sample.txt";
        options.MimeType = "text/plain";
    });

    if (uploadResult.IsSuccess)
    {
        Console.WriteLine($"   File uploaded: {uploadResult.Value!.Name}");
        Console.WriteLine($"   Size: {uploadResult.Value.Length} bytes");
        Console.WriteLine($"   URI: {uploadResult.Value.Uri}");
    }
    else
    {
        Console.WriteLine($"   Upload failed");
    }
    Console.WriteLine();

    // Upload a binary file
    Console.WriteLine("3. Uploading a binary file...");
    var binaryData = new byte[1024];
    Random.Shared.NextBytes(binaryData);
    var binaryUploadResult = await storage.UploadAsync(binaryData, options =>
    {
        options.FileName = "random-data.bin";
        options.Directory = "binary-files";
    });

    if (binaryUploadResult.IsSuccess)
    {
        Console.WriteLine($"   Binary file uploaded: {binaryUploadResult.Value!.FullName}");
    }
    Console.WriteLine();

    // Check if file exists
    Console.WriteLine("4. Checking if file exists...");
    var existsResult = await storage.ExistsAsync("sample.txt");
    Console.WriteLine($"   sample.txt exists: {existsResult.Value}");
    Console.WriteLine();

    // Get file metadata
    Console.WriteLine("5. Getting file metadata...");
    var metadataResult = await storage.GetBlobMetadataAsync("sample.txt");
    if (metadataResult.IsSuccess)
    {
        var metadata = metadataResult.Value!;
        Console.WriteLine($"   Name: {metadata.Name}");
        Console.WriteLine($"   Full Name: {metadata.FullName}");
        Console.WriteLine($"   Size: {metadata.Length} bytes");
        Console.WriteLine($"   MIME Type: {metadata.MimeType}");
        Console.WriteLine($"   Created: {metadata.CreatedOn}");
        Console.WriteLine($"   Modified: {metadata.LastModified}");
    }
    Console.WriteLine();

    // List all files
    Console.WriteLine("6. Listing all files...");
    var files = storage.GetBlobMetadataListAsync();
    var count = 0;
    await foreach (var file in files)
    {
        Console.WriteLine($"   - {file.FullName} ({file.Length} bytes)");
        count++;
    }
    Console.WriteLine($"   Total files: {count}");
    Console.WriteLine();

    // Download the file
    Console.WriteLine("7. Downloading file...");
    var downloadResult = await storage.DownloadAsync("sample.txt");
    if (downloadResult.IsSuccess)
    {
        await using var localFile = downloadResult.Value!;
        // Use LocalFile's built-in method which properly closes the stream first
        var content = await localFile.ReadAllTextAsync();
        Console.WriteLine($"   Downloaded content: {content.Substring(0, Math.Min(50, content.Length))}...");
    }
    Console.WriteLine();

    // Get file as stream
    Console.WriteLine("8. Getting file as stream...");
    var streamResult = await storage.GetStreamAsync("sample.txt");
    if (streamResult.IsSuccess)
    {
        using var stream = streamResult.Value!;
        using var reader = new StreamReader(stream);
        var streamContent = await reader.ReadToEndAsync();
        Console.WriteLine($"   Stream content length: {streamContent.Length} characters");
    }
    Console.WriteLine();

    // Delete files
    Console.WriteLine("9. Deleting files...");
    var deleteResult1 = await storage.DeleteAsync("sample.txt");
    Console.WriteLine($"Deleted sample.txt: IsSuccess={deleteResult1.IsSuccess}, Value={deleteResult1.Value}");

    var deleteResult2 = await storage.DeleteAsync(options =>
    {
        options.FileName = "random-data.bin";
        options.Directory = "binary-files";
    });
    Console.WriteLine($"Deleted random-data.bin: IsSuccess={deleteResult2.IsSuccess}, Value={deleteResult2.Value}");
    Console.WriteLine();

    // Delete directory
    Console.WriteLine("10. Deleting directory...");
    var deleteDirResult = await storage.DeleteDirectoryAsync("binary-files");
    Console.WriteLine($"   Directory deleted: {deleteDirResult.IsSuccess}");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("All operations completed successfully!");
    Console.ResetColor();
}

async Task RunDemoModeAsync()
{
    Console.WriteLine("Demo Mode - Simulating operations:");
    Console.WriteLine();

    Console.WriteLine("1. Create container (folder)");
    Console.WriteLine("   -> Would create folder 'ManagedCode-Storage-Sample' in Google Drive");
    Console.WriteLine();

    Console.WriteLine("2. Upload text file");
    Console.WriteLine("   -> Would upload 'sample.txt' with content");
    Console.WriteLine();

    Console.WriteLine("3. Upload binary file");
    Console.WriteLine("   -> Would upload 'random-data.bin' to 'binary-files/' directory");
    Console.WriteLine();

    Console.WriteLine("4. Check file exists");
    Console.WriteLine("   -> Would check if 'sample.txt' exists");
    Console.WriteLine();

    Console.WriteLine("5. Get metadata");
    Console.WriteLine("   -> Would retrieve file metadata (name, size, dates, etc.)");
    Console.WriteLine();

    Console.WriteLine("6. List files");
    Console.WriteLine("   -> Would list all files in the container");
    Console.WriteLine();

    Console.WriteLine("7. Download file");
    Console.WriteLine("   -> Would download 'sample.txt' to local temp file");
    Console.WriteLine();

    Console.WriteLine("8. Get stream");
    Console.WriteLine("   -> Would get file content as a stream");
    Console.WriteLine();

    Console.WriteLine("9. Delete files");
    Console.WriteLine("   -> Would delete 'sample.txt' and 'random-data.bin'");
    Console.WriteLine();

    Console.WriteLine("10. Delete directory");
    Console.WriteLine("    -> Would delete 'binary-files' directory");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("To run with real Google Drive operations, set up the environment variables as described above.");
    Console.ResetColor();

    await Task.CompletedTask;
}

