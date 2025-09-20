## Conversations
any resulting updates to agents.md should go under the section "## Rules to follow"
When you see a convincing argument from me on how to solve or do something. add a summary for this in agents.md. so you learn what I want over time.
If I say any of the following point, you do this: add the context to agents.md, and associate this with a specific type of task.
if I say "never do x" in some way.
if I say "always do x" in some way.
if I say "the process is x" in some way.
If I tell you to remember something, you do the same, update


## Rules to follow
TBA

# Repository Guidelines

## Project Structure & Module Organization
ManagedCode.Storage.slnx orchestrates the .NET 9 projects. Core abstractions live in `ManagedCode.Storage.Core/`. Providers sit under `Storages/ManagedCode.Storage.*` with one project per cloud target (Azure, AWS, GCP, FileSystem, Sftp). Integration surfaces, including the ASP.NET server and client SDKs, live in `Integraions/`. Test doubles stay in `ManagedCode.Storage.TestFakes/`, while the suites in `Tests/ManagedCode.Storage.Tests/` are grouped into ASP.NET flows, provider runs, and shared helpers. Keep shared assets such as `logo.png` at the repository root.

## Build, Test, and Development Commands
Run `dotnet restore ManagedCode.Storage.slnx` before compiling. Use `dotnet build ManagedCode.Storage.slnx` to compile every target and surface analyzer warnings. Execute all tests with `dotnet test Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release`. For coverage, run `dotnet test /p:CollectCoverage=true /p:CoverletOutput=coverage /p:CoverletOutputFormat=opencover`. Use `dotnet format ManagedCode.Storage.slnx` before opening a pull request.

## Coding Style & Naming Conventions
Follow standard C# conventions: 4-space indentation, PascalCase types, camelCase locals, and suffix async APIs with `Async`. Nullability is enabled repository-wide, so annotate optional members and avoid the suppression operator unless justified. Match method names to existing patterns such as `DownloadFile_WhenFileExists_ReturnsSuccess`. Remove unused usings and let analyzers guide layout.

## Testing Guidelines
Tests use xUnit and FluentAssertions; choose `[Fact]` for atomic cases and `[Theory]` for data-driven permutations. Place provider suites under `Tests/ManagedCode.Storage.Tests/Storages/` and reuse `.../Common/` helpers to spin up Testcontainers (Azurite, LocalStack, FakeGcsServer). Add fakes or harnesses mirroring `ManagedCode.Storage.TestFakes/` when introducing new providers. Always run `dotnet test` locally and exercise critical upload/download paths.

## Commit & Pull Request Guidelines
Write commit subjects in the imperative mood (`add ftp retry policy`) and keep them provider-scoped. Group related edits in one commit and avoid WIP spam. Pull requests should summarize impact, list touched projects, reference issues, and note new configuration or secrets. Include the `dotnet` commands you ran and add logs when CI needs context.

## Security & Configuration Tips
Never commit API keys, connection strings, or `.trx` artifacts; rely on environment variables or user secrets. Document minimal permissions and default container expectations for new providers. Ensure server integrations stay authenticated and refresh configuration examples in `README.md` when behavior changes.
