# OneDrive integration plan

- [x] Reference the official `Microsoft.Graph` SDK and configure `GraphServiceClient` injection through `OneDriveStorageOptions`.
- [x] Implement `IOneDriveClient` plus `GraphOneDriveClient` to mirror upload, download, metadata, and listing APIs documented for Microsoft Graph drives.
- [x] Create `OneDriveStorage` that adapts `BaseStorage` to OneDrive paths, normalizes root prefixes, and returns `BlobMetadata` compatible with the shared abstractions.
- [x] Provide DI-friendly `OneDriveStorageProvider` so ASP.NET and worker hosts can register the provider alongside keyed/default storage bindings.
- [ ] Add sample ASP.NET controller snippets showing how to request delegated or app-only permissions and pass a configured `GraphServiceClient` into `OneDriveStorageOptions`.
- [ ] Extend tests with `IOneDriveClient` mocks that mirror Graph responses for uploads, downloads, listings, deletion, and metadata resolution.
- [ ] Document user-facing setup: Azure App Registration, scopes (`Files.ReadWrite.All`), and the minimal token acquisition steps for CLI and ASP.NET hosts.
