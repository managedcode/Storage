Project: ManagedCode.Storage
Stack: .NET 10 / C# / xUnit / VSTest / Coverlet / GitHub Pages docs

Follows [MCAF](https://mcaf.managed-code.com/)

---

## Purpose

This file defines how AI agents work in this solution.

- Root `AGENTS.md` holds the global workflow, shared commands, cross-cutting rules, maintainability limits, and global skill catalog.
- Every `.csproj` root in this multi-project solution keeps a local `AGENTS.md` with project-specific entry points, boundaries, commands, risks, and applicable skills.
- Local `AGENTS.md` files may tighten root rules, but they must not weaken them silently.

## Solution Topology

- Solution root: `ManagedCode.Storage.slnx`
- Projects or modules with local `AGENTS.md` files:
  - `ManagedCode.Storage.Core/`
  - `ManagedCode.Storage.TestFakes/`
  - `ManagedCode.Storage.VirtualFileSystem/`
  - `Integraions/ManagedCode.Storage.Client/`
  - `Integraions/ManagedCode.Storage.Client.SignalR/`
  - `Integraions/ManagedCode.Storage.Orleans/`
  - `Integraions/ManagedCode.Storage.Server/`
  - `Storages/ManagedCode.Storage.Aws/`
  - `Storages/ManagedCode.Storage.Browser/`
  - `Storages/ManagedCode.Storage.Azure/`
  - `Storages/ManagedCode.Storage.Azure.DataLake/`
  - `Storages/ManagedCode.Storage.CloudKit/`
  - `Storages/ManagedCode.Storage.Dropbox/`
  - `Storages/ManagedCode.Storage.FileSystem/`
  - `Storages/ManagedCode.Storage.Google/`
  - `Storages/ManagedCode.Storage.GoogleDrive/`
  - `Storages/ManagedCode.Storage.OneDrive/`
  - `Storages/ManagedCode.Storage.Sftp/`
  - `Tests/ManagedCode.Storage.Tests/`
  - `Tests/ManagedCode.Storage.BrowserServerHost/`
  - `Tests/ManagedCode.Storage.BrowserWasmHost/`

## Rule Precedence

1. Read the solution-root `AGENTS.md` first.
2. Read the nearest local `AGENTS.md` for the area you will edit.
3. Apply the stricter rule when both files speak to the same topic.
4. Local `AGENTS.md` files may refine or tighten root rules, but they must not silently weaken them.
5. If a local rule needs an exception, document it explicitly in the nearest local `AGENTS.md`, ADR, or feature doc.

## Conversations (Self-Learning)

Learn the user's stable habits, preferences, and corrections. Record durable rules here instead of relying on chat history.

Before doing any non-trivial task, evaluate the latest user message.
If it contains a durable rule, correction, preference, or workflow change, update `AGENTS.md` first.
If it is only task-local scope, do not turn it into a lasting rule.

Update this file when the user gives:

- a repeated correction
- a permanent requirement
- a lasting preference
- a workflow change
- a high-signal frustration that indicates a rule was missed

Extract rules aggressively when the user says things equivalent to:

- "never", "don't", "stop", "avoid"
- "always", "must", "make sure", "should"
- "remember", "keep in mind", "note that"
- "from now on", "going forward"
- "the workflow is", "we do it like this"

Preferences belong in `## Preferences`:

- positive preferences go under `Likes`
- negative preferences go under `Dislikes`
- comparisons should become explicit rules or preferences

Corrections should update an existing rule when possible instead of creating duplicates.

Treat these as strong signals and record them immediately:

- anger, swearing, sarcasm, or explicit frustration
- ALL CAPS, repeated punctuation, or "don't do this again"
- the same mistake happening twice
- the user manually undoing or rejecting a recurring pattern

Do not record:

- one-off instructions for the current task
- temporary exceptions
- requirements that are already captured elsewhere without change

Rule format:

- one instruction per bullet
- place it in the right section
- capture the why, not only the literal wording
- remove obsolete rules when a better one replaces them

## Global Skills

List only the skills this solution actually uses.

- `mcaf-solution-governance` — use when bootstrapping or refining root and local `AGENTS.md`, maintainability limits, rule precedence, or solution topology.
- `mcaf-architecture-overview` — use when creating or updating `docs/Architecture.md` after module, boundary, or contract changes.
- `mcaf-documentation` — use for durable docs, docs-site synchronization, Mermaid-heavy docs updates, and repo documentation structure changes.
- `mcaf-adr-writing` — use when documenting cross-cutting architectural or standards decisions in `docs/ADR/`.
- `mcaf-feature-spec` — use when documenting non-trivial feature behavior in `docs/Features/`.
- `mcaf-dotnet` — entry skill for .NET work and routing to specialized `.NET` skills.
- `mcaf-dotnet-analyzer-config` — use when the repo-root `.editorconfig` or analyzer severity ownership changes.
- `mcaf-dotnet-code-analysis` — use when SDK analyzer policy in `Directory.Build.props` or project files changes.
- `mcaf-dotnet-features` — use when modern C# or .NET 10 feature choices matter.
- `mcaf-testing` — use for scenario coverage planning and verification strategy.
- `mcaf-dotnet-netarchtest` — use when architecture dependency rules in `Tests/ManagedCode.Storage.Tests/Architecture/` change.
- `mcaf-dotnet-xunit` — use for xUnit tests in `Tests/ManagedCode.Storage.Tests/`.
- `mcaf-dotnet-quality-ci` — use for the repo quality pass and CI-aligned verification.
- `mcaf-dotnet-complexity` — use when work risks breaching file, type, function, or nesting limits.
- `mcaf-dotnet-coverlet` — use when coverage commands, thresholds, or coverage tooling change.
- `mcaf-dotnet-format` — use when formatter or analyzer command wiring changes.
- `mcaf-solid-maintainability` — use when reshaping responsibilities or SOLID boundaries.
- `mcaf-ci-cd` — use for GitHub Actions, branch protection, and `build-and-test` workflow changes.

If the stack is `.NET`, follow these skill-management rules explicitly:

- `mcaf-dotnet` is the entry skill and routes to specialized `.NET` skills.
- Keep exactly one framework skill: this repo uses `mcaf-dotnet-xunit`.
- Add tool-specific `.NET` skills only when the repository actually uses those tools in CI or local verification.
- Keep only `mcaf-*` skills in repository-local agent skill directories.
- When upgrading skills, recheck `restore`, `build`, `test`, `format`, and `coverage` commands against the repo toolchain.

## Rules to Follow (Mandatory, no Exceptions)

### Commands

- `restore`: `dotnet restore ManagedCode.Storage.slnx`
- `build`: `dotnet build ManagedCode.Storage.slnx`
- `test`: `dotnet test Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release`
- `coverage`: `dotnet test Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release /p:CollectCoverage=true /p:CoverletOutput=coverage /p:CoverletOutputFormat=opencover`
- `format`: `dotnet format ManagedCode.Storage.slnx`

Toolchain notes:

- Tests run on `xUnit` over `VSTest` via `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`.
- `format` intentionally applies fixes instead of running in verify-only mode.
- CI verifies formatting with `dotnet format ManagedCode.Storage.slnx --verify-no-changes`.
- `coverage` uses `coverlet.msbuild` through `dotnet test` MSBuild properties.
- Architecture dependency rules use `NetArchTest.Rules` inside `Tests/ManagedCode.Storage.Tests/Architecture/` and run through the normal `test` command.
- Explicit `LangVersion` should only be introduced if a project intentionally differs from the SDK default.

### Project AGENTS Policy

- Multi-project solutions MUST keep one root `AGENTS.md` plus one local `AGENTS.md` in each project root.
- Each local `AGENTS.md` MUST document:
  - project purpose
  - entry points
  - boundaries
  - local commands
  - applicable skills
  - local risks or protected areas
- Keep provider and integration local files focused on public DI extensions, public contracts, and boundary-specific risks.
- If a project grows enough that the root file becomes vague, add or tighten the local `AGENTS.md` before continuing implementation.

### Maintainability Limits

- `file_max_loc`: `400`
- `type_max_loc`: `200`
- `function_max_loc`: `50`
- `max_nesting_depth`: `3`
- `exception_policy`: `Document any justified exception in the nearest ADR, feature doc, or local AGENTS.md with the reason, scope, and removal or refactor plan.`

### Task Delivery

- Read the assignment, inspect code and docs, and define scope before planning.
- Start from `docs/Architecture.md` and the nearest local `AGENTS.md`.
- Treat `docs/Architecture.md` as the architecture map for every non-trivial task.
- If the architecture map is missing, stale, or diagram-free, update it before implementation.
- Define scope before coding:
  - in scope
  - out of scope
- For non-trivial work, create a root-level `<slug>.plan.md` file before making code or doc changes.
- Keep the plan file current until the task is complete; it must track ordered steps, risks, baseline failures, verification steps, and done criteria.
- Write a multi-step plan before implementation.
- Implement code and tests together.
- Run verification in layers:
  - changed tests
  - related suite
  - broader regressions
- After all required tests pass, run `format`, then `build`.
- Summarize changes, risks, and test results before marking the task complete.
- Always run required builds and tests yourself; do not ask the user to execute them.

### Documentation

- Docs live in `docs/` and `README.md`.
- `docs/Architecture.md` is the required global map and the first stop for agents.
- Keep a GitHub Pages docs site in sync with `docs/`, using `DOCS-EXAMPLE/` as the reference template for structure and CI or pipeline behavior.
- Keep `docs/templates/ADR-Template.md` and `docs/templates/Feature-Template.md` aligned with the current MCAF references.
- When adding new docs pages under `docs/Features/`, `docs/ADR/`, or `docs/API/`, also update the corresponding `index.md` so the page is discoverable in the docs catalog and navigation.
- Docs site navigation must not include a `Templates` page.
- When referencing repo file paths in docs, make them clickable with the corresponding GitHub `blob` or `tree` URL.
- Update docs when behavior changes.
- Update configuration examples when required.
- Documentation must include clear schemas or diagrams, preferably Mermaid, for every non-trivial feature and integration.
- When adding new projects or providers, ensure `README.md` clearly documents installation, DI wiring, and basic usage examples.
- Where feasible, prefer provider options that can build vendor SDK clients from credentials while still allowing client injection for advanced scenarios.
- Avoid ownership flags like `ownsClient`; prefer a clear wrapper or factory boundary so lifetime and disposal rules stay predictable.
- For providers that rely on vendor SDK clients, document how to obtain credentials, keys, or tokens and include a minimal code snippet that builds the required SDK client instance.
- CloudKit docs must explicitly clarify that `ContainerId` is a CloudKit container identifier, not a secret, and document the optional `HttpClient` and `ICloudKitClient` injection points.
- Credentials docs should keep provider sections consistent: What you need, Typical steps, Minimal SDK or DI snippet, Suggested configuration keys.
- Keep the testing strategy discoverable from `docs/Development/setup.md`.
- Validate all Mermaid diagrams against the docs site renderer version `10.9.5` and fix any syntax errors before shipping docs changes.
- Docs site must include `sitemap.xml` and reference it from `robots.txt`.
- Docs site must display the project version from `Directory.Build.props`, not CI run numbers.
- Docs site footer should keep copyright, license, sitemap, and version compact and preferably single-line.
- Docs site should display the short project name `Storage` in the site title or nav while keeping `ManagedCode.Storage` in package-name content.
- Do not add ADRs for docs-site generation or pipeline changes; document docs-site build, SEO, and GitHub Pages workflow details under `docs/Development/` instead.
- Docs site must not generate redirect or alias pages like `/Storage/`; keep a single canonical home URL.
- After changing the generator, workflow, or layout, smoke-check the built HTML is not empty.

### Testing

- Every behavior change needs sufficient automated tests to cover its cases; one test is the minimum, not the target.
- Each public API endpoint has at least one test; complex endpoints need tests for different inputs and errors.
- Integration tests must exercise real flows end-to-end, not just call endpoints in isolation.
- Prefer integration or API tests over isolated unit tests.
- Do not use fakes in automated tests; validate behavior through real runtimes, browser sessions, or containerized dependencies.
- For browser-storage scenarios, verify through handwritten Razor-based test apps in both Blazor WebAssembly and Interactive Server, driven by Playwright against real browser state.
- When browser storage behavior could race across pages or tabs, add explicit concurrency coverage.
- Never write tests that only validate mocked interactions; every test must assert concrete, observable behavior such as state, output, errors, or side effects.
- When third-party systems must be simulated, prefer real protocol-compatible test infrastructure over in-process fakes.
- No mocks for internal systems such as databases, queues, caches, or browser runtimes.
- Mocks are allowed only as a last resort for external third-party systems when no real or containerized alternative exists, and that exception must be called out explicitly in the task summary.
- Never delete or weaken a test to make it pass.
- Each test must verify a real flow or scenario; tests without meaningful assertions are forbidden.
- Check coverage to find gaps, not to chase a number.
- Tests use `xUnit` + `Shouldly`; choose `[Fact]` for atomic cases and `[Theory]` for data-driven permutations.
- Tests run on `VSTest`; do not mix in `Microsoft.Testing.Platform` assumptions.
- Coverage uses the repo-defined `coverlet.msbuild` flow and must not regress without a written exception.
- Place provider suites under `Tests/ManagedCode.Storage.Tests/Storages/` and reuse `Tests/ManagedCode.Storage.Tests/Common/` helpers for Testcontainers infrastructure such as Azurite, LocalStack, and FakeGcsServer.
- For browser providers, put end-to-end Playwright coverage in `Tests/ManagedCode.Storage.Tests/Storages/Browser/` and keep the executable test hosts under `Tests/ManagedCode.Storage.BrowserServerHost/` and `Tests/ManagedCode.Storage.BrowserWasmHost/`.

### Storage Platform

- Ensure storage-related changes keep broad automated coverage around 85-90% using generic, provider-agnostic tests across file systems, storages, and integrations.
- Deliver ASP.NET integrations that expose upload or download controllers, SignalR streaming, and matching HTTP and SignalR clients built on the storage layer for files, streams, and chunked transfers.
- Provide base ASP.NET controllers with minimal routing so consumers can inherit and customize routes, authorization, and behavior without rigid defaults.
- Favor controller extension patterns and optionally expose interfaces to guide consumers toward recommended controller actions.
- For comprehensive storage-platform upgrades, follow the nine-step flow: harden SignalR streaming, harden controller upload paths, add keyed DI registrations and cross-provider sync fixtures, extend VFS with keyed support and large-file trials, create streamed large-file or CRC helpers, run end-to-end suites, verify Blazor upload extensions, expand docs with VFS and provider identity guidance, and finish with the full preview-enabled test suite.
- Normalize MIME lookups through `MimeHelper`; avoid ad-hoc MIME resolution helpers so all content-type logic flows through its APIs.

### Project Structure

- `ManagedCode.Storage.slnx` orchestrates the .NET 10 projects.
- Keep the canonical `AGENTS.md` in the repository root; for multi-project solutions add or update local `AGENTS.md` files per project so project-specific guidance stays close to each codebase slice.
- Core abstractions live in `ManagedCode.Storage.Core/`.
- The virtual file system lives in `ManagedCode.Storage.VirtualFileSystem/`.
- Providers live in `Storages/ManagedCode.Storage.*`.
- Integrations live in `Integraions/`.
- Test doubles live in `ManagedCode.Storage.TestFakes/`.
- Tests live in `Tests/ManagedCode.Storage.Tests/`.
- Keep shared assets such as `logo.png` at the repository root.

### Autonomy

- Start work immediately with no permission-seeking.
- Ask questions only for architecture blockers not covered by docs or ADRs.
- Report only when the task is complete.

### Tooling

- When installing or updating MCAF assets, install only current skills with the `mcaf-` prefix so repository automation stays aligned with the maintained MCAF skill set and avoids stale skill drift.

### Code Style

- The repo-root `.editorconfig` is the source of truth for formatting, naming, style, and analyzer severity.
- Use NuGet Central Package Management via `Directory.Packages.props`; keep package versions out of individual `.csproj` files so versions stay aligned across the repository.
- Follow standard C# conventions: 4-space indentation, PascalCase types, camelCase locals.
- Nullability is enabled: annotate optional members and avoid `!` unless justified.
- Suffix async APIs with `Async`; keep test names aligned with existing patterns such as `DownloadFile_WhenFileExists_ReturnsSuccess`.
- Remove unused usings and let analyzers guide layout.
- When a `foreach` loop starts by transforming the iteration variable, prefer mapping the sequence explicitly with `.Select(...)` so the intent is clearer.
- Avoid buffering whole files into `MemoryStream` in product code; assume multi-GB files and stream directly to the destination while using incremental hashing or CRC when verification is needed.
- Stream capability properties such as `CanSeek`, `CanWrite`, `Length`, and `Position` must reflect the real backing stream or selected strategy; do not hardcode capability flags in a way that changes stream semantics silently.
- No magic literals; extract them to constants, enums, configuration, or dedicated value types when it improves clarity.

### Git And PRs

- Write commit subjects in the imperative mood such as `add ftp retry policy` and keep them provider-scoped.
- Group related edits in one commit and avoid WIP spam.
- PRs should summarize impact, list touched projects, reference issues, and note new configuration or secrets.
- Include the `dotnet` commands you ran and add logs when CI needs context.
- Keep a required CI check named `build-and-test` running on every PR and push to `main` so branch protection always receives a status.

### Critical

- Never commit secrets, keys, access tokens, or connection strings.
- Never commit `.trx` artifacts.
- Never mock internal systems in integration tests; use containers or fakes instead.
- Never skip tests to make a branch green.
- Never force-push to `main`.
- Never approve or merge on behalf of a human maintainer.

### Boundaries

Always:

- Read root and local `AGENTS.md` files before editing code.
- Read the relevant docs before changing behavior or architecture.
- Run the required verification commands yourself.

Ask first:

- changing public API contracts
- adding new dependencies
- modifying database schema
- deleting code files

## Preferences

### Likes

- Repository-facing docs, especially `README.md`, should stay in English and describe only the current supported behavior, not transitional legacy or fallback paths.
- Temporary root-level `*.plan.md` files should be removed once a task is complete and their contents are no longer needed.

### Dislikes

- Template-generated scaffolding in tests; keep test hosts and verification surfaces minimal, hand-written, and purpose-built.
- Unnecessary product-code fallbacks; prefer one clear production path unless backward compatibility is an explicit requirement for the task.
