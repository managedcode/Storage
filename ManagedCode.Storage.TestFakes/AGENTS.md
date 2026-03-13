Project: ManagedCode.Storage.TestFakes
Owned by: ManagedCode.Storage maintainers

Parent: `../AGENTS.md`

## Purpose

- Provides fake storage implementations and helper extensions used by the repository integration tests.
- Exists so tests can exercise storage flows without coupling production packages to test-only doubles.

## Entry Points

- `FakeAWSStorage.cs`
- `FakeAzureStorage.cs`
- `FakeGoogleStorage.cs`
- `MockCollectionExtensions.cs`

## Boundaries

- In scope: test-only storage doubles and helper registration utilities
- Out of scope: production package behavior, public NuGet-facing APIs, and docs-site content
- Protected or high-risk areas: behavioral parity with the real provider contracts used by the test suite

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.TestFakes.csproj`
- `test`: `dotnet test ../Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release`
- `format`: `dotnet format ../ManagedCode.Storage.slnx`
- Active test framework: `xUnit`
- Runner model: `VSTest`
- Analyzer severity lives in the repo-root `.editorconfig`.

## Applicable Skills

- `mcaf-dotnet`
- `mcaf-testing`
- `mcaf-dotnet-xunit`
- `mcaf-dotnet-quality-ci`

## Local Constraints

- Stricter maintainability limits: none; inherit the root defaults.
- Required local docs: `docs/Architecture.md`, `README.md`, and the nearest feature or ADR docs when public behavior changes.
- Local exception policy: inherit the root `exception_policy` and document any project-specific exception in the nearest ADR, feature doc, or local `AGENTS.md`.

## Local Rules

- Keep fake behavior aligned with the public provider contracts and external API shapes that tests rely on.
- Do not let test-only helpers leak into production projects or samples.
- Prefer small targeted fakes over broad behavior switches that make assertions ambiguous.
