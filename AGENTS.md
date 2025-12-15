ManagedCode.Storage — .NET 10

Follows [MCAF](https://mcaf.managed-code.com/)

---

## Conversations (Self-Learning)

Learn the user's habits, preferences, and working style. Extract rules from conversations, save to "## Rules to follow", and generate code according to the user's personal rules.

**Update requirement (core mechanism):**

Before doing ANY task, evaluate the latest user message.  
If you detect a new rule, correction, preference, or change → update `AGENTS.md` first.  
Only after updating the file you may produce the task output.  
If no new rule is detected → do not update the file.

**When to extract rules:**

- prohibition words (never, don't, stop, avoid) or similar → add NEVER rule
- requirement words (always, must, make sure, should) or similar → add ALWAYS rule
- memory words (remember, keep in mind, note that) or similar → add rule
- process words (the process is, the workflow is, we do it like) or similar → add to workflow
- convincing argument about approach → capture as a rule (include why)
- future words (from now on, going forward) or similar → add permanent rule

**Preferences → add to Preferences section:**

- positive (I like, I prefer, this is better) or similar → Likes
- negative (I don't like, I hate, this is bad) or similar → Dislikes
- comparison (prefer X over Y, use X instead of Y) or similar → preference rule

**Corrections → update or add rule:**

- error indication (this is wrong, incorrect, broken) or similar → fix and add rule
- repetition frustration (don't do this again, you ignored, you missed) or similar → emphatic rule
- manual fixes by user → extract what changed and why

**Strong signal (add IMMEDIATELY):**

- swearing, frustration, anger, sarcasm → critical rule
- ALL CAPS, excessive punctuation (!!!, ???) → high priority
- same mistake twice → permanent emphatic rule
- user undoes your changes → understand why, prevent

**Ignore (do NOT add):**

- temporary scope (only for now, just this time, for this task) or similar
- one-off exceptions
- context-specific instructions for current task only

**Rule format:**

- One instruction per bullet
- Tie to category (Testing, Code, Docs, etc.)
- Capture WHY, not just what
- Remove obsolete rules when superseded

---

## Rules to follow (Mandatory, no exceptions)

### Commands

- restore: `dotnet restore ManagedCode.Storage.slnx`
- build: `dotnet build ManagedCode.Storage.slnx`
- test: `dotnet test Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release`
- coverage: `dotnet test Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release /p:CollectCoverage=true /p:CoverletOutput=coverage /p:CoverletOutputFormat=opencover`
- format: `dotnet format ManagedCode.Storage.slnx`

### Task Delivery (ALL TASKS)

- Read assignment, inspect code and docs before planning
- Write multi-step plan before implementation
- Implement code and tests together
- Run tests in layers: new → related suite → broader regressions
- After all tests pass: run format, then build
- Summarize changes and test results before marking complete
- Always run required builds and tests yourself; do not ask the user to execute them (explicit user directive)

### Documentation (ALL TASKS)

- Docs live in `docs/` and `README.md`
- Keep a GitHub Pages docs site in sync with `docs/`, using `DOCS-EXAMPLE/` as the reference template for structure and CI/pipeline
- When adding new docs pages under `docs/Features/`, `docs/ADR/`, or `docs/API/`, also update the corresponding `index.md` to link the page so it’s discoverable in the docs catalog/navigation (the site generator will still publish the page even without the link)
- Docs site navigation: do not include a `Templates` page (keep templates in-repo, but don’t surface a dedicated docs-site section for them)
- Docs content: when referencing repo file paths (e.g. `Tests/.../X.cs`), make them clickable by linking to the corresponding GitHub `blob/tree` URL
- Update docs when behaviour changes
- Update configuration examples when required
- Documentation must include clear schemas/diagrams (prefer Mermaid) for every non-trivial feature and integration so GitHub users can understand flows quickly
- When adding new projects/providers, ensure `README.md` clearly documents installation, DI wiring, and basic usage examples
- Where feasible, prefer provider options that can build vendor SDK clients from credentials (to reduce consumer boilerplate) while still allowing client injection for advanced scenarios
- Avoid "ownership flags" like `ownsClient`; prefer a clear swap point (wrapper/factory) so lifetime and disposal rules stay simple and predictable
- For providers that rely on vendor SDK clients (Graph/Drive/Dropbox/etc.), document how to obtain credentials/keys/tokens and include a minimal code snippet that builds the required SDK client instance
- CloudKit docs: explicitly clarify that `ContainerId` is a CloudKit container identifier (not a secret) tied to an Apple App ID, and document the optional `HttpClient`/`ICloudKitClient` injection points for advanced HTTP customization and testing
- Credentials docs: keep provider sections consistent (What you need → Typical steps → Minimal SDK/DI snippet → Suggested configuration keys) so consumers can wire providers without guesswork
- Docs: keep the testing strategy discoverable from `docs/Development/setup.md` (link or embedded section) so users find how/why tests run during initial setup
- Docs: validate all Mermaid diagrams render on the docs site (Mermaid v10.9.5) and fix any syntax errors before shipping docs changes
- Docs site: include `sitemap.xml` and reference it from `robots.txt` so search engines can discover all pages after every rebuild
- Docs site: display the project version from `Directory.Build.props` (not CI run numbers) and keep the footer copyright line slightly smaller than the rest for a cleaner visual hierarchy
- Docs site: display the short project name `Storage` in the site title/nav (keep `ManagedCode.Storage` in content where it refers to package IDs)
- Docs: do not add ADRs for docs-site generation/pipeline changes; document the docs-site build, SEO, and GitHub Pages workflow under `docs/Development/` instead
- Docs site: do not generate redirect/alias pages like `/Storage/`; keep a single canonical home URL (`/`) and remove unused routes

### Testing (ALL TASKS)

- Every behaviour change needs sufficient automated tests to cover its cases; one is the minimum, not the target
- Each public API endpoint has at least one test; complex endpoints have tests for different inputs and errors
- Integration tests must exercise real flows end-to-end, not just call endpoints in isolation
- Prefer integration/API tests over unit tests
- Keep mocks to an absolute minimum; prefer real flows using fakes/containers where possible
- Never write tests that only validate mocked interactions; every test must assert concrete, observable behaviour (state, output, errors, side-effects)
- When faking external APIs, match the official API docs (endpoints, status codes, error payloads, and field naming) and prefer `HttpMessageHandler`-based fakes over ad-hoc mocks
- No mocks for internal systems (DB, queues, caches) — use containers/fakes as appropriate
- Mocks only for external third-party systems
- Never delete or weaken a test to make it pass
- Each test verifies a real flow or scenario; tests without meaningful assertions are forbidden
- Check coverage to find gaps, not to chase numbers
- Tests use xUnit + Shouldly; choose `[Fact]` for atomic cases and `[Theory]` for data-driven permutations
- Place provider suites under `Tests/ManagedCode.Storage.Tests/Storages/` and reuse `Tests/ManagedCode.Storage.Tests/Common/` helpers to spin up Testcontainers (Azurite, LocalStack, FakeGcsServer)
- Add fakes or harnesses in `ManagedCode.Storage.TestFakes/` when introducing new providers

### Storage Platform (ALL TASKS)

- Ensure storage-related changes keep broad automated coverage around 85-90% using generic, provider-agnostic tests across file systems, storages, and integrations
- Deliver ASP.NET integrations that expose upload/download controllers, SignalR streaming, and matching HTTP and SignalR clients built on the storage layer for files, streams, and chunked transfers
- Provide base ASP.NET controllers with minimal routing so consumers can inherit and customize routes, authorization, and behaviors without rigid defaults
- Favor controller extension patterns and optionally expose interfaces to guide consumers on recommended actions so they can implement custom endpoints easily
- For comprehensive storage platform upgrades, follow the nine-step flow: solidify SignalR streaming hub/client with logging and tests, harden controller upload paths (standard/stream/chunked) with large-file coverage, add keyed DI registrations and cross-provider sync fixtures, extend VFS with keyed support and >1 GB trials, create streamed large-file/CRC helpers, run end-to-end suites (controllers, SignalR, VFS, cross-provider), verify Blazor upload extensions, expand docs with VFS + provider identity guidance + keyed samples, and finish by running the full preview-enabled test suite addressing warnings
- Normalise MIME lookups through `MimeHelper`; avoid ad-hoc MIME resolution helpers so all content-type logic flows through its APIs

### Project Structure

- `ManagedCode.Storage.slnx` orchestrates the .NET 10 projects
- Core abstractions: `ManagedCode.Storage.Core/`
- Virtual file system: `ManagedCode.Storage.VirtualFileSystem/`
- Providers: `Storages/ManagedCode.Storage.*` (one project per cloud target: Azure, AWS, GCP, FileSystem, Sftp)
- Integrations (ASP.NET server + client SDKs): `Integraions/`
- Test doubles: `ManagedCode.Storage.TestFakes/`
- Test suites: `Tests/ManagedCode.Storage.Tests/` (ASP.NET flows, provider runs, shared helpers)
- Keep shared assets such as `logo.png` at repository root

### Autonomy

- Start work immediately — no permission seeking
- Questions only for architecture blockers not covered by ADR
- Report only when task is complete

### Code Style

- Style rules: `.editorconfig`
- Follow standard C# conventions: 4-space indentation, PascalCase types, camelCase locals
- Nullability is enabled: annotate optional members; avoid `!` unless justified
- Suffix async APIs with `Async`; keep test names aligned with existing patterns (e.g., `DownloadFile_WhenFileExists_ReturnsSuccess`)
- Remove unused usings and let analyzers guide layout
- When a `foreach` loop’s first step is just transforming the iteration variable (e.g., `var y = Map(x)`), prefer mapping the sequence explicitly with `.Select(...)` so intent is clearer and analyzers stay quiet
- Avoid buffering whole files into `MemoryStream` in product code (assume multi‑GB files); stream directly to the destination (response stream / file stream) and use incremental hashing/CRC when you need verification
- No magic literals — extract to constants, enums, or config when it improves clarity

### Git & PRs

- Write commit subjects in the imperative mood (`add ftp retry policy`) and keep them provider-scoped
- Group related edits in one commit and avoid WIP spam
- PRs should summarize impact, list touched projects, reference issues, and note new configuration or secrets
- Include the `dotnet` commands you ran and add logs when CI needs context
- Keep a required CI check named `build-and-test` running on every PR and push to `main` so branch protection always receives a status (it’s worse for merges if the check is missing/never reported than if it runs and fails)

### Critical (NEVER violate)

- Never commit secrets, keys, access tokens, or connection strings
- Never commit `.trx` artifacts
- Never mock internal systems in integration tests (DB, queues, caches) — use containers/fakes instead
- Never skip tests to make PR green
- Never force push to `main`
- Never approve or merge (human decision)

### Boundaries

**Always:**

- Read `AGENTS.md` and relevant docs before editing code
- Run tests before commit

**Ask first:**

- Changing public API contracts
- Adding new dependencies
- Modifying database schema
- Deleting code files

---

## Preferences

### Likes

### Dislikes
