Project: ManagedCode.Storage.Cartograph
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Exposes files catalogued in immutable Cartograph artifacts through the normal Storage abstraction.
- Keeps the Cartograph dependency and mapped-memory lifetime rules isolated in a separate provider package.

## Entry Points

- `CartographStorage.cs`
- `ICartographStorage.cs`
- `CartographStorageProvider.cs`
- `Options/CartographStorageOptions.cs`
- `Extensions/ServiceCollectionExtensions.cs`
- `Extensions/StorageFactoryExtensions.cs`
- `CartographReadStream.cs`
- `CartographDownload.cs`

## Boundaries

- In scope: local artifact reading, catalog validation, file metadata/listing/downloads, provider options, default/typed/keyed DI, and factory creation.
- Out of scope: changing Core contracts, duplicating the Cartograph format/parser, modifying immutable artifacts, remote transports, or test fakes.
- Protected areas: lease lifetime, untrusted catalog paths and record spans, cancellation, checksum verification, and DI alias identity.

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.Cartograph.csproj`
- `test`: `dotnet test ../../Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release --filter "FullyQualifiedName~Cartograph"`
- `format`: `dotnet format ../../ManagedCode.Storage.slnx`
- Framework and runner: xUnit v2 / VSTest; analyzer policy comes from the root `.editorconfig`.

## Applicable Skills

- `mcaf-dotnet`
- `mcaf-testing`
- `mcaf-dotnet-xunit`
- `mcaf-architecture-overview`
- `mcaf-documentation`

## Local Rules And Risks

- Use the centrally pinned published `Cartograph.Catalog` package and its transitive Format/substrate packages.
- Returned streams own an independent artifact snapshot and dispose every record lease before its artifact.
- Never extract whole files to temporary storage or load their payload into `MemoryStream` to implement reads.
- Keep per-record checksums enabled. Validate catalog entry spans and lengths against record-directory metadata without reading file payloads.
- Resolve catalog-relative names with ordinal comparison. Ignore `SourceRoot`; catalog paths must never become host filesystem paths.
- Mutations, legal holds, and container deletion return explicit unsupported results. `CreateContainerAsync` validates an existing artifact without creating one.
- Downloads stream to a sibling staging file and replace the destination only after successful checksum-verified reading; always clean up staging files and preserve an existing destination on failure.
- Required documentation: `docs/Architecture.md`, `docs/Features/provider-cartograph.md`, and the README usage section.
- Inherit root maintainability limits and exception policy.
