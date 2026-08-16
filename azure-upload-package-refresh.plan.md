# Azure upload and package refresh plan

## Scope

### In scope

- Stop Azure uploads from issuing a second blob-properties request after a successful upload.
- Preserve complete `BlobMetadata` from the committed upload and the caller-owned upload options.
- Add Azure provider regression coverage against real Azurite behavior.
- Update all direct NuGet package versions to current stable releases.
- Bump every package from `10.0.6` to `10.0.7`.
- Run focused, related, and repository-wide verification.
- Commit and push the release change so the repository release workflow can publish the packages.
- Update Prostir to the published `10.0.7` storage packages and verify its affected test lane.

### Out of scope

- Public storage API redesign.
- Changes to non-Azure provider behavior beyond dependency compatibility fixes required by the package refresh.
- Skipping or weakening existing tests.

## Risks

- The upload response does not expose every property returned by a later blob-properties request.
- Broad dependency updates can introduce source or runtime incompatibilities.
- Release publication depends on the repository's GitHub Actions and NuGet credentials.

## Ordered steps

1. Record the current outdated direct-package report and identify stable target versions.
2. Change Azure upload metadata construction and add regression coverage.
3. Update central package versions and the repository package version.
4. Run focused Azure tests, the non-browser-stress suite, formatting, and the full solution build.
5. Review the final diff, commit, and push `main`.
6. Wait for the release workflow and verify `ManagedCode.Storage.Azure 10.0.7` is published.
7. Update Prostir storage package versions and run its focused verification.

## Baseline failures

- Production `ManagedCode.Storage.Azure 10.0.6` can log and return `BlobNotFound` from `GetBlobMetadataInternalAsync` after the preceding Azure upload request succeeded.

## Done criteria

- Azure upload succeeds without a post-upload `GetPropertiesAsync` call.
- Returned metadata includes path, name, URI, container, content length, timestamps, caller metadata, and MIME type.
- Direct NuGet packages are current according to the final outdated report, or any exception is explicitly recorded.
- Required tests, formatting verification, and build are green.
- Version `10.0.7` is committed, pushed, and published.
- Prostir consumes the published storage packages and its affected tests pass.

## Verification record

- Focused Azure upload suite: 16 passed, 0 failed.
- Non-browser-stress suite: 467 passed, 1 failed, 3 skipped; the sole failure was a Playwright concurrent-tab status timeout outside the changed Azure/package code.
- Isolated retry of that Playwright test: 1 passed, 0 failed.
- `dotnet format ManagedCode.Storage.slnx --verify-no-changes`: passed.
- `dotnet list ManagedCode.Storage.slnx package --outdated`: no outdated direct packages.
- `dotnet list ManagedCode.Storage.slnx package --vulnerable --include-transitive`: no vulnerable packages.
- `dotnet build ManagedCode.Storage.slnx --configuration Release`: passed with 0 warnings and 0 errors.
