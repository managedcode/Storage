# Google Drive integration plan

- [x] Reference the official `Google.Apis.Drive.v3` client and thread it through `GoogleDriveStorageOptions`.
- [x] Build `IGoogleDriveClient` with a Drive-service backed implementation that honors folder hierarchies, metadata fields, and official upload/download patterns.
- [x] Adapt `GoogleDriveStorage` to produce `BlobMetadata` results and operate through the shared `BaseStorage` contract.
- [ ] Provide quick-start instructions for OAuth client configuration, service account usage, and refresh-token setup for console and ASP.NET apps.
- [ ] Expand tests with deterministic `IGoogleDriveClient` fakes that simulate Drive folder traversal, file uploads, range downloads, deletions, and metadata fetches.
- [ ] Add docs showing the minimal Drive scopes (`https://www.googleapis.com/auth/drive.file`) and how to inject authenticated `DriveService` instances via DI.
