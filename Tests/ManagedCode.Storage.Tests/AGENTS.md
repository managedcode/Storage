Project: ManagedCode.Storage.Tests
Owned by: ManagedCode.Storage maintainers

Parent: `../../AGENTS.md`

## Purpose

- Holds the repository integration, transport, provider, Orleans, and VFS test suites.
- Exists to prove real end-to-end storage behavior across providers and integrations using xUnit, Testcontainers, and test fakes where appropriate.

## Entry Points

- `AspNetTests/`
- `Storages/`
- `VirtualFileSystem/`
- `Common/`
- `ManagedCode.Storage.Tests.csproj`

## Boundaries

- In scope: test scenarios, shared harnesses, container-backed integration flows, and regression coverage for public behavior
- Out of scope: production package implementation except through public contracts and sanctioned test helpers
- Protected or high-risk areas: provider matrix coverage, testcontainer lifecycle helpers, and assertions that define the platform quality bar

## Project Commands

- `build`: `dotnet build ManagedCode.Storage.Tests.csproj`
- `test`: `dotnet test ManagedCode.Storage.Tests.csproj --configuration Release --filter "Category!=BrowserStress"`
- `browser-stress`: `dotnet test ManagedCode.Storage.Tests.csproj --configuration Release --filter "Category=BrowserStress"`
- `coverage`: `dotnet test ManagedCode.Storage.Tests.csproj --configuration Release --filter "Category!=BrowserStress" /p:CollectCoverage=true /p:CoverletOutput=coverage /p:CoverletOutputFormat=opencover`
- `format`: `dotnet format ../../ManagedCode.Storage.slnx`
- Active test framework: `xUnit`
- Runner model: `VSTest`
- Analyzer severity lives in the repo-root `.editorconfig`.

## Applicable Skills

- `mcaf-dotnet`
- `mcaf-testing`
- `mcaf-dotnet-netarchtest`
- `mcaf-dotnet-xunit`
- `mcaf-dotnet-quality-ci`
- `mcaf-dotnet-complexity`
- `mcaf-dotnet-coverlet`

## Local Constraints

- Stricter maintainability limits: none; inherit the root defaults.
- Required local docs: `docs/Architecture.md`, `README.md`, and the nearest feature or ADR docs when public behavior changes.
- Local exception policy: inherit the root `exception_policy` and document any project-specific exception in the nearest ADR, feature doc, or local `AGENTS.md`.

## Local Rules

- Prefer real flows with Testcontainers and `ManagedCode.Storage.TestFakes`; do not replace end-to-end coverage with interaction-only mocks.
- Every new public behavior needs integration coverage that asserts observable outcomes, not only successful method calls.
- Keep architecture dependency rules in `Architecture/` focused on durable package boundaries so they stay stable as implementation details move.
- Do not weaken assertions or skip suites to get a green run; fix the real regression or document an explicit exception.
- Browser stress tests are an explicit lane, not hidden debt: keep them automated via the dedicated `browser-stress` command or workflow instead of folding them into the fast default test path.
