# Repo Quality Baseline Plan

## Scope

- In scope:
  - audit the current .NET quality stack across the solution
  - add a repo-root `.editorconfig` as the main analyzer/style ownership file
  - tighten SDK analyzer and complexity configuration without conflicting with current CPM work
  - add executable architecture tests for durable dependency rules
  - align AGENTS and CI quality commands where the repo actually enables the gates
  - run quality verification and record outcomes
- Out of scope:
  - public API changes
  - provider behavior refactors unrelated to the quality baseline
  - wholesale formatter migration away from `dotnet format` unless the repo already wants it

## Baseline

- Root `.editorconfig` is missing.
- `Directory.Build.props` already enables `EnableNETAnalyzers`.
- The repo currently uses `dotnet format` as the formatting command.
- `cloc` and `roslynator` are available globally; local tool manifest is missing.
- `csharpier` is not installed.
- `NetArchTest.Rules` is not referenced yet.
- Many files exceed the repo maintainability limit of 400 LOC.
- Worktree is dirty with unrelated user changes; edits must avoid overwriting them.

## Risks

- Adding aggressive analyzer severity may surface a large warning backlog.
- The repo is mid-flight on Orleans and Central Package Management changes.
- Architecture tests must target durable boundaries only, or they will create churn.
- CI updates must preserve the required `build-and-test` check name.

## Steps

1. Capture the current quality and CI/tooling baseline.
2. Add the root `.editorconfig` and explicit complexity or analyzer ownership.
3. Add missing architecture-test coverage with minimal, durable rules.
4. Align AGENTS or workflow commands only where the repo now truly supports them.
5. Run verification in layers:
   - targeted tests
   - solution test command
   - format
   - build
   - extra gates actually enabled in this repo
6. Summarize findings, residual risks, and not-applicable tools.

## Verification

- `dotnet test Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release --filter ...`
- `dotnet test Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release`
- `dotnet format ManagedCode.Storage.slnx`
- `dotnet build ManagedCode.Storage.slnx`
- `roslynator analyze ManagedCode.Storage.slnx --severity-level warning`
- `cloc --vcs=git --include-lang='C#,MSBuild,JSON,XML,YAML,Markdown'`

## Done Criteria

- Root analyzer ownership is explicit and checked in.
- Architecture drift has at least one executable guard.
- Quality commands in docs and CI match the configured tooling.
- Verification results are captured with clear remaining debt.
