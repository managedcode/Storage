# Google Drive Storage Sample

This sample application demonstrates how to use the `ManagedCode.Storage.GoogleDrive` library.

## Prerequisites

1. **Google Cloud Project**

   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select an existing one

2. **Enable Google Drive API**

   - Navigate to "APIs & Services" > "Library"
   - Search for "Google Drive API" and enable it

3. **Create Service Account**

   - Go to "APIs & Services" > "Credentials"
   - Click "Create Credentials" > "Service Account"
   - Fill in the details and create the account
   - Click on the service account, go to "Keys" tab
   - Add a new JSON key and download it

4. **Share a folder with the Service Account**
   - In Google Drive, create a folder or use an existing one
   - Right-click the folder > "Share"
   - Add the service account email (found in the JSON key file as `client_email`)
   - Give it "Editor" access

## Configuration

### Required: Folder ID

You must specify the folder ID where files will be stored:

```powershell
# PowerShell
$env:GOOGLE_DRIVE_FOLDER_ID = "1abc123def456..."
```

```bash
# Bash/Linux
export GOOGLE_DRIVE_FOLDER_ID=1abc123def456...
```

### Finding the Folder ID

Open the folder in Google Drive, the URL will be like:
`https://drive.google.com/drive/folders/1abc123def456...`

The ID is the part after `/folders/`

### Credentials: Option 1 - JSON Content (Recommended for Production)

Use the actual JSON content of your service account key. This is ideal for cloud deployments, CI/CD, and containerized environments.

```powershell
# PowerShell - Set the JSON content directly
$env:GOOGLE_SERVICE_ACCOUNT_JSON = '{"type":"service_account","project_id":"your-project",...}'

# Or read from file and set as environment variable
$env:GOOGLE_SERVICE_ACCOUNT_JSON = Get-Content -Path "C:\path\to\key.json" -Raw
```

```bash
# Bash/Linux - Set the JSON content
export GOOGLE_SERVICE_ACCOUNT_JSON='{"type":"service_account","project_id":"your-project",...}'

# Or read from file
export GOOGLE_SERVICE_ACCOUNT_JSON=$(cat /path/to/key.json)
```

### Credentials: Option 2 - File Path (Convenient for Local Development)

Point to the JSON key file on disk:

```powershell
# PowerShell
$env:GOOGLE_SERVICE_ACCOUNT_JSON_PATH = "C:\path\to\your-service-account-key.json"
```

```bash
# Bash/Linux
export GOOGLE_SERVICE_ACCOUNT_JSON_PATH=/path/to/your-service-account-key.json
```

## Running the Sample

```bash
cd samples/GoogleDriveSample
dotnet run
```

## What the Sample Does

1. **Creates a container** - Uses the specified folder ID
2. **Uploads a text file** - Uploads "sample.txt" with some content
3. **Uploads a binary file** - Uploads "random-data.bin" to a subdirectory
4. **Checks file existence** - Verifies the uploaded file exists
5. **Gets file metadata** - Retrieves file information (name, size, dates)
6. **Lists all files** - Enumerates all files in the container
7. **Downloads file** - Downloads the text file to a local temp file
8. **Gets file as stream** - Reads file content as a stream
9. **Deletes files** - Removes the uploaded files
10. **Deletes directory** - Removes the subdirectory

## Troubleshooting

### "Request had insufficient authentication scopes"

- The service account may need additional permissions
- Ensure the folder is shared with the service account email

### "File not found"

- Check that the folder ID is correct
- Verify the service account has access to the folder

### "Access denied"

- Make sure the service account has "Editor" access to the folder

### "FolderId is required"

- You must set the `GOOGLE_DRIVE_FOLDER_ID` environment variable

## Using in Your Own Application

```csharp
// 1. Add the NuGet package (when published) or project reference
// <PackageReference Include="ManagedCode.Storage.GoogleDrive" Version="x.x.x" />

// 2. Configure services - Option A: Using JSON content directly (recommended)
services.AddGoogleDriveStorageAsDefault(options =>
{
    options.ServiceAccountJson = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON");
    options.FolderId = "your-folder-id"; // REQUIRED - Get this from the folder URL
});

// 2. Configure services - Option B: Using file path
services.AddGoogleDriveStorageAsDefault(options =>
{
    options.ServiceAccountJsonPath = "path/to/key.json";
    options.FolderId = "your-folder-id"; // REQUIRED
});

// 3. Inject and use
public class MyService
{
    private readonly IGoogleDriveStorage _storage;

    public MyService(IGoogleDriveStorage storage)
    {
        _storage = storage;
    }

    public async Task UploadFileAsync(Stream content, string fileName)
    {
        var result = await _storage.UploadAsync(content, opt => opt.FileName = fileName);
        if (result.IsSuccess)
        {
            Console.WriteLine($"Uploaded: {result.Value.Uri}");
        }
    }
}
```
