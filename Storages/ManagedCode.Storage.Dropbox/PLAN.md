# Dropbox integration plan

- [x] Reference the official `Dropbox.Api` SDK and expose injection through `DropboxStorageOptions`.
- [x] Implement `IDropboxClientWrapper` with a wrapper over `DropboxClient` that aligns with documented upload, download, list, and metadata APIs.
- [x] Connect `DropboxStorage` to the shared abstractions and normalize path handling for custom root prefixes.
- [ ] Add user guidance for creating an app in Dropbox, generating access tokens, and scoping permissions for file access.
- [ ] Build mocks for `IDropboxClientWrapper` that mirror Dropbox metadata shapes so tests can validate uploads, downloads, and deletions without network calls.
- [ ] Provide DI samples (keyed and default) so ASP.NET apps can register Dropbox storage with configuration-bound options.
